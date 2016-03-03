﻿namespace Myriad

open System

type MeasureBuilder(map : Map<String, Dimension>) =
    member x.Bind(m, f) =  m |> List.collect f |> Set.ofList
    member x.Zero() = Set.empty
    member x.Yield(m) = Set.ofList [ Measure.Create(map.[fst(m)], snd(m)) ]
    member x.YieldFrom(m) = m
    member x.For(m,f) = x.Bind(m,f)
    member x.Combine(a, b) = Set.union a b
    member x.Delay(f) = f()

/// Orders a sequence of clusters by dimension
type PropertyBuilder(dimensions : Dimension seq) =
    // Dimension Id -> weight
    let weights = dimensions |> Seq.mapi (fun i d -> d.Id, int (2.0 ** float i)) |> Map.ofSeq
    
    let compareMeasures (x : Cluster) (y : Cluster) = 
        let xWeight = x.Measures |> Seq.sumBy (fun m -> weights.[m.Dimension.Id])
        let yWeight = y.Measures |> Seq.sumBy (fun m -> weights.[m.Dimension.Id])
        yWeight.CompareTo(xWeight)
    
    member x.Create(key : String) (timestamp : Int64) (clusters : Cluster seq) =
        let clustersByWeight = clusters |> Set.ofSeq |> Seq.toList |> List.sortWith compareMeasures 
        Property(key, timestamp, clustersByWeight)
        
