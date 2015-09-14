﻿namespace Myriad

open System

type Operation = None = 0 | Create = 1 | Update = 2 | Delete = 3

type Audit =
    { Timestamp : Int64; UpdateUser : String; Operation : Operation }

type Dimension = 
    { Id : Int64; Name : String; Audit : Audit }
    override x.ToString() = String.Concat("Dimension [", x.Name, "] [", x.Id, "]")

type Property = 
    { Id : Int64; Name : String }
    override x.ToString() = String.Concat("Property [", x.Name, "] [", x.Id, "]")

/// Measures are equivalent over dimension id and value
[<CustomEquality;CustomComparison>]
type Measure = 
    struct
        val DimensionId : Int64
        val DimensionName : String
        val Value : String 
        new(dimensionId : Int64, dimensionName : String, value : String) = 
            { DimensionId = dimensionId; DimensionName = dimensionName; Value = value}
        new(dimension : Dimension, value : String) = 
            { DimensionId = dimension.Id; DimensionName = dimension.Name; Value = value}
    end

    override x.ToString() = String.Format("'{0}' [{1}] = '{2}'", x.DimensionName, x.DimensionId, x.Value)

    override x.Equals(obj) = 
        match obj with
        | :? Measure as y -> Measure.CompareTo(x, y) = 0
        | _ -> false

    override x.GetHashCode() = hash(x.DimensionId, x.Value)
    
    static member CompareTo(x : Measure, y : Measure) = compare (x.DimensionId, x.Value) (y.DimensionId, y.Value)

    interface IComparable with
        member x.CompareTo other = 
            match other with 
            | :? Measure as y -> Measure.CompareTo(x, y)
            | _ -> invalidArg "other" "cannot compare value of different types" 

/// Clusters are equivalent over their measures set
[<CustomEquality;CustomComparison>]
type Cluster = 
    struct
        val Id : Int64        
        val Property : Property
        val Value : String       
        val Measures : Set<Measure> 
        val Audit : Audit
        new(id : Int64, property : Property, value : String, measures : Set<Measure>, audit : Audit) =
            { Id = id; Property = property; Value = value; Measures = measures; Audit = audit }
    end

    override x.Equals(obj) = 
        match obj with
        | :? Cluster as y -> (x.Measures = y.Measures)
        | _ -> false

    override x.GetHashCode() = hash(x.Measures)
    
    static member CompareTo(x : Set<Measure>, y : Set<Measure>) = compare x y

    interface IComparable with
        member x.CompareTo other = 
            match other with 
            | :? Cluster as y -> Cluster.CompareTo(x.Measures, y.Measures)
            | _ -> invalidArg "other" "cannot compare value of different types" 


type Context = { AsOf : DateTimeOffset; Measures : Set<Measure> }

[<CustomEquality;CustomComparison>]
type ClusterSet = 
    struct     
        val Timestamp : Int64;
        val Clusters : Set<Cluster>
        new(timestamp : Int64, clusters : Set<Cluster>) = { Timestamp = timestamp; Clusters = clusters }
    end
    
    override x.Equals(yobj) = 
        match yobj with
        | :? ClusterSet as y -> (x.Timestamp = y.Timestamp)
        | _ -> false

    override x.GetHashCode() = hash(x)
    
    interface IComparable with
        member x.CompareTo other = 
            match other with 
            | :? ClusterSet as y -> x.Timestamp.CompareTo(y.Timestamp)
            | _ -> invalidArg "other" "cannot compare value of different types" 




