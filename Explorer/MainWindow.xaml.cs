﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reactive;
using System.Windows;
using System.Windows.Input;

using Myriad;
using Myriad.Client;

namespace Myriad.Explorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MyriadReader _reader;
        private MyriadWriter _writer;
        private readonly DataSet _dataSet = new DataSet("ResultsView");
        private readonly List<DimensionValues> _dimensionValues = new List<DimensionValues>();

        public MainWindow()
        {
            InitializeComponent();

            NavigationControl.Subscribe(Observer.Create<Uri>(OnRefresh));
            ContextControl.Subscribe(Observer.Create<List<DimensionValues>>(OnQuery));
            ContextControl.Subscribe(Observer.Create<Measure>(OnMeasure));
            ContextControl.IsEnabled = false;

            _dataSet.Tables.Add(new DataTable("Results"));
            DataContext = _dataSet.Tables[0].DefaultView;
        }

        private void OnRefresh(Uri uri)
        {
            ResetClient(uri);
        }

        private void OnQuery(List<DimensionValues> dimensionValuesList)
        {
            var result = _reader.Query(dimensionValuesList);

            var table = _dataSet.Tables[0];
            table.Clear();

            foreach (var map in result)
            {
                var row = table.NewRow();

                foreach (var pair in map)
                {
                    if (table.Columns.Contains(pair.Key) == false)
                        continue;

                    var column = table.Columns[pair.Key];

                    if (column.DataType == typeof(DateTimeOffset))
                    {
                        var timestamp = Int64.Parse(pair.Value);
                        row[pair.Key] = Epoch.ToDateTimeOffset(timestamp);
                    }
                    else
                        row[pair.Key] = pair.Value;
                }

                table.Rows.Add(row);
            }
        }

        private void OnMeasure(Measure measure)
        {
            //_reader.AddDimensionValue(measure.Dimension, measure.Value);
        }

        private void ResetResults()
        {
            var table = new DataTable("Results");

            var dimensions = _reader.GetDimensionList();
            dimensions.Insert(0, "Ordinal");
            dimensions.Insert(2, "Value");
            dimensions.Add("UserName");
            dimensions.Add("Timestamp");

            Func<string, Type> getColumnType =
                d =>
                {
                    switch (d)
                    {
                        case "Ordinal":
                            return typeof(int);
                        case "Timestamp":
                            return typeof(DateTimeOffset);
                        default:
                            return typeof(string);
                    }
                };

            dimensions.ForEach(d => table.Columns.Add(d, getColumnType(d)));


            _dataSet.Reset();
            _dataSet.Tables.Add(table);
            DataContext = _dataSet.Tables[0].DefaultView;
        }

        private void ResetClient(Uri uri)
        {
            _reader = new MyriadReader(uri);
            _writer = new MyriadWriter(uri);

            ResetResults();

            _dimensionValues.Clear();
            _dimensionValues.AddRange(_reader.GetMetadata());
            ContextControl.Reset(_dimensionValues);
            ContextControl.IsEnabled = true;
        }

        /// <summary>Convert a data row view to a cluster</summary>
        private static Cluster ToCluster(DataRowView view, List<DimensionValues> dimensionValues)
        {
            var measures = new HashSet<Measure>(
                dimensionValues
                    .Where(d => d.Dimension.Name != "Property" && string.IsNullOrEmpty(view[d.Dimension.Name].ToString()) == false)
                    .Select(d => new Measure(d.Dimension, view[d.Dimension.Name].ToString()))
            );

            var timestamp = Epoch.GetOffset(((DateTimeOffset)view["Timestamp"]).Ticks);
            return Cluster.Create(view["Value"].ToString(), measures, view["UserName"].ToString(), timestamp);
        }

        private void OnResultsDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if( ResultView.SelectedItems.Count == 0 )
                return;

            var view = ResultView.SelectedItems[0] as DataRowView;
            if (view == null)
                return;

            var valueMap = _dimensionValues.ToDictionary(d => d.Dimension.Name, d => view[d.Dimension.Name].ToString());

            // Get Property
            var response = _reader.GetProperties(new[] {valueMap["Property"]});
            var property = response.Properties.FirstOrDefault();

            var editor = new PropertyEditorWindow
            {
                Owner = this,
                Property = property,
                Cluster = ToCluster(view, _dimensionValues),
                ValueMap = valueMap,
                Dimensions = _dimensionValues
            };

            var result = editor.ShowDialog();
            if (result.HasValue == false || result.Value == false)
                return;

            var newProperty = _writer.PutProperty(editor.GetPropertyOperation());
        }
    }
}