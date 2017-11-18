/// <reference path="../Core/PageBase.ts" />

namespace RIFF.Web.System {
    interface DataBrowserOptions {
        urlGetAggregateQuery: string,
        urlGetDetailQuery: string,
        urlSourceDataQuery: string,
        dataSources: any[]
    }

    export class DataBrowserPage extends RIFF.Web.Core.PageBase {
        constructor(private options: DataBrowserOptions) {
            super();

            this.dataSource = new DevExpress.data.DataSource({
                load: (loadOptions) => {
                    var d = $.Deferred();

                    if (!this.currentSource)
                    {
                        return null;
                    }

                    $.post(this.options.urlGetAggregateQuery, { options: this.queryOptions(loadOptions) }, function (data) {
                    }).done(function (result) {
                        $('#footerContainer').show();
                        $('#statusContainer').text('Retrieved ' + RIFFWebCore.Helpers.numberWithCommas(result.count, 0) + ' rows in ' + RIFFWebCore.Helpers.numberWithCommas(result.time, 0) + 'ms.');
                        $('#statusContainer').data('sql', result.sql);
                            // You can process the received data here
                            d.resolve(result.data, {});
                        });
                    return d.promise();
                }
            });
        }

        onInitialize() {
            $('#sourcebox').dxSelectBox(<DevExpress.ui.dxSelectBoxOptions>{
                dataSource: this.options.dataSources,
                displayExpr: 'Description',
                onValueChanged: (e) => this.switchSource(e.value)
            });
        }

        private dataSource: DevExpress.data.DataSource;

        queryOptions(loadOptions: DevExpress.data.LoadOptions) {
            return {
                Breakdown: this.currentBreakdowns,
                Data: this.currentIncludes,
                Source: this.currentSource.Code,
                //Group: (loadOptions && loadOptions.group) ? JSON.stringify(loadOptions.group) : "",
                Filter: (loadOptions && loadOptions.filter) ? JSON.stringify(loadOptions.filter) : ""
            };
        }

        public showSQL() {
            DevExpress.ui.dialog.alert('<pre>' + $('#statusContainer').data('sql') + '</pre>', 'SQL View');
        }

        switchSource(source: any) {

            $('#aggregateContainer').show();
            $('#includeContainer').show();

            this.currentSource = source;

            var aggregators = source.Columns.filter(function (c, i, a) {
                return c.CanGroup && !c.Mandatory;
            });

            var includers = source.Columns.filter(function (c, i, a) {
                return c.Aggregator && !c.Mandatory;
            });

            $('#dataContainer').dxDataGrid(<DevExpress.ui.dxDataGridOptions>{
                dataSource: this.dataSource,//[],
                remoteOperations: {
                    filtering: true,
                    sorting: false,
                    paging: false,
                    grouping: false, // true
                    summary: false
                },
                onCellPrepared: function (e: any) {
                    if (e.column.dataType == 'number') {
                        if (e.value > 0)
                            e.cellElement.addClass('positive');
                        else if (e.value < 0)
                            e.cellElement.addClass('negative');
                    }
                },
                columns: this.getColumns(false),
                summary: this.getSummary(),
                filterRow: {
                    visible: true
                },
                rowAlternationEnabled: true,
                headerFilter: {
                    visible: true
                },
                groupPanel: {
                    visible: true
                },
                scrolling: {
                    mode: 'virtual',
                    preloadEnabled: true,
                    showScrollbar: 'always',
                    useNative: true
                },
                pager: { visible: false },
                paging: { enabled: false },
                allowColumnReordering: true,
                allowColumnResizing: true,
                columnResizingMode: 'widget',
                hoverStateEnabled: true,
                export: {
                    enabled: true
                },
                searchPanel: { visible: true },
                masterDetail: {
                    enabled: true,
                    template: (container, info) => {
                        var col = this.getColumns(true);
                        $.post(this.options.urlGetDetailQuery, { options: this.queryOptions(null), group: JSON.stringify(info.row.data) }, function (result) {
                            $('<div>').dxDataGrid(<DevExpress.ui.dxDataGridOptions> {
                                hoverStateEnabled: true,
                                columnAutoWidth: true,
                                dataSource: result.data,
                                columns: col,
                                allowColumnReordering: true,
                                allowColumnResizing: true,
                                columnFixing: {
                                    enabled: true
                                },
                                pager: { visible: false },
                                paging: { enabled: false },
                                headerFilter: { visible: true },
                                rowAlternationEnabled: true,
                                selection: { mode: 'single' },
                                height: '350px',
                                hint: 'Source data rows',
                                scrolling: {
                                    mode: 'virtual',
                                    preloadEnabled: true,
                                    useNative: true
                                },
                                export: {
                                    enabled: true
                                },
                                onCellPrepared: function (e: any) {
                                    if (e.column.dataType == 'number') {
                                        if (e.value > 0)
                                            e.cellElement.addClass('positive');
                                        else if (e.value < 0)
                                            e.cellElement.addClass('negative');
                                    }
                                },
                            }).appendTo(container);
                        });
                    }
                }
            });

            $('#aggregatebox').dxList(<DevExpress.ui.dxListOptions>{
                dataSource: aggregators,
                selectionMode: 'multiple',
                showSelectionControls: true,
                pageLoadMode: 'scrollBottom',
                keyExpr: 'Code',
                itemTemplate: function (itemData, itemIndex, itemElement) {
                    itemElement.append('<span title="' + (itemData.Description ? itemData.Description : itemData.Caption) + '">' + itemData.Caption + '</span>');
                },
                onSelectionChanged: (data) => this.refreshData(),
            });

            $('#includebox').dxList(<DevExpress.ui.dxListOptions>{
                dataSource: includers,
                selectionMode: 'multiple',
                showSelectionControls: true,
                pageLoadMode: 'scrollBottom',
                keyExpr: 'Code',
                itemTemplate: function (itemData, itemIndex, itemElement) {
                    itemElement.append('<span title="' + (itemData.Description ? itemData.Description : itemData.Caption) + '">' + itemData.Caption + '</span>');
                },
                onSelectionChanged: (data) => this.refreshData()
            });

            this.refreshData();
        }

        getColumnVisibility = function (breakdowns: string[], includes: string[]): any[] {
            return this.currentSource.Columns
                .map(function (c, i, a) {
                    return {
                        code: c.Code, visible: breakdowns.indexOf(c.Code) >= 0 || includes.indexOf(c.Code) >= 0 || c.Mandatory
                    }
                });
        }

        getDefaultWidth = function (c: any): string {
            switch (c.Format)
            {
                case 'date':
                    return '120px';
                case 'string':
                    return '175px';
                case 'number':
                    default:
                    return '120px';
            }
        }

        getDefaultAlignment = function (c: any): string {
            switch (c.Format) {
                case 'date':
                    return 'center';
                default:
                    return null;
            }
        }

        getColumns = function (visible: boolean): any[] {
            return this.currentSource.Columns
                .map((c, i, a) => {
                    return {
                        dataField: c.Code,
                        caption: visible ? c.Caption : ((c.Aggregator == "Sum" ? 'Σ ' : '') + c.Caption),
                        visible: visible,
                        width: visible ? null : this.getDefaultWidth(c),
                        alignment: this.getDefaultAlignment(c),
                        allowFiltering: c.Format == 'date',//!c.Aggregator,
                        allowHeaderFiltering: !c.Aggregator && c.Format != 'date',
                        format: c.Format == 'date' ? 'yyyy-MM-dd' : c.Format,
                        dataType: c.Format == 'date' ? 'date' : (c.Format == 'string' ? 'string' : 'number')
                    };
                }).concat({ dataField: '__RowCount', visible: true, allowFiltering: false, allowHeaderFiltering: false, allowSorting: false, dataType: 'number', caption: '# Rows', alignment: 'right' });
        }

        getSummary = function (): any {
            var summaries = this.currentSource.Columns
                .filter(function (c, i, a) {
                    return c.Aggregator;
                })
                .map(function (c, i, a) {
                    return {
                        column: c.Code,
                        summaryType: 'sum',
                        valueFormat: c.Format,
                        alignByColumn: true,
                        showInColumn: true
                    };
                });

            return {
                totalItems: summaries,
                groupItems: summaries,
                texts: {
                    sum: "{0}"
                }
            };
        }

        private currentSource: any;
        private currentBreakdowns: any;
        private currentIncludes: any;

        refreshData() {
            this.currentBreakdowns = $('#aggregatebox').dxList('instance').option('selectedItems').map(function (c, i, a) {
                return c.Code;
            });

            this.currentIncludes = $('#includebox').dxList('instance').option('selectedItems').map(function (c, i, a) {
                return c.Code;
            });

            var visibility = this.getColumnVisibility(this.currentBreakdowns, this.currentIncludes);

            var grid = $('#dataContainer').dxDataGrid('instance');
            visibility.forEach(function (v, i, a) {
                grid.columnOption(v.code, 'visible', v.visible);
            });

            grid.refresh();
        }

        onInitialized() {
        }
    }
}
