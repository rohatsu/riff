/// <reference path="../../typings/codemirror/index.d.ts" />
/// <reference path="../Core/PageBase.ts" />

namespace RIFF.Web.System {
    interface DataEditorOptions {
        urlInvalidateEntry: string
        urlGetDocumentForEdit: string
        urlDataSet: string
        urlExportEntry: string
        urlDownloadEntry: string
        urlUpdateDocument: string
        urlSaveDocument: string
        urlGetLatestDocuments: string
        urlGetInstances: string
        initialDate: Date
    }

    export class DataEditorPage extends RIFF.Web.Core.PageBase {
        constructor(private options: DataEditorOptions) {
            super();
        }

        onInitialize() {
            this.contentEditor = (CodeMirror as any).fromTextArea($('#contenteditorbox')[0] as HTMLTextAreaElement, {
                lineNumbers: false,
                lineWrapping: true,
                mode: "xml",
                extraKeys: {
                    "F11": function (cm) {
                        cm.setOption("fullScreen", !cm.getOption("fullScreen"));
                    },
                    "Esc": function (cm) {
                        if (cm.getOption("fullScreen")) cm.setOption("fullScreen", false);
                    }
                },
                matchTags: { bothTags: true },
                foldGutter: true,
                gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter"]
            });

            this.keyEditor = (CodeMirror as any).fromTextArea($('#keyeditorbox')[0] as HTMLTextAreaElement, {
                lineNumbers: false,
                lineWrapping: true,
                mode: "xml"
            });

            this.keyEditor.setSize(null, "230px");
            this.contentEditor.setSize(null, $(window).height() - 410);

            $('#datebox').dxDateBox({
                min: null,
                max: new Date(),
                width: '80px',
                onFocusIn: function (e) {
                    e.component.option("max", new Date());
                },
                displayFormat: 'yyyy-MM-dd',
                value: this.options.initialDate,
                onValueChanged: (e) => this.refreshGrid((codeBehind as RIFF.Web.System.DataEditorPage).getValueDate())
            });

            $('#deletebutton').dxButton({ text: "Delete", onClick: () => this.deleteKey(), icon: 'trash' });
            $('#viewbutton').dxButton({ text: "Table", onClick: () => this.viewKey(), icon: 'find' });
            $('#xmlbutton').dxButton({ text: "XML", onClick: () => this.xmlKey(), icon: 'download' });
            $('#excelbutton').dxButton({ text: "Excel", onClick: () => this.excelKey(), icon: 'download' });
            $('#editbutton').dxButton({ text: "Edit", onClick: () => this.edit(), icon: 'edit' });
            $('#updatebutton').dxButton({ text: "Update", onClick: () => this.update(), icon: 'save', type: 'default' });
            $('#savebutton').dxButton({ text: "Save", onClick: () => this.save(), icon: 'save', type: 'default' });
            $('#copybutton').dxButton({ text: "Copy", onClick: () => this.copy(), icon: 'add' });
            $('#importbutton').dxButton({ text: "Import", onClick: () => this._import(), icon: 'upload' });
            $('#savebutton').hide();
            $('#updatebutton').hide();
            this.contentTypeBox = $('#contenttypebox').dxTextBox({ disabled: true }).data("dxTextBox");
            this.keyTypeBox = $('#keytypebox').dxTextBox({ disabled: true }).data("dxTextBox");

            this.refreshGrid(null); // latest initially
        };

        onInitialized() {
        }

        groupCellTemplate(groupCell, info) {
            $('<div>').html(info.text).css('font-weight', 'bold').appendTo(groupCell);
        }

        public contentEditor: any
        public keyEditor: any
        editingKeyType: any
        editingContentType: any
        editingKeyReference: any
        keyTypeBox: any
        contentTypeBox: any

        editRow(keyType, contentType, keyReference) {
            $('#savebutton').hide();
            $('#updatebutton').hide();
            $('#editbutton').show();
            $('#copybutton').show();

            RIFFWebCore.RIFFPage.showLoadPanel();

            $.getJSON(this.options.urlGetDocumentForEdit, { type: keyType, keyReference: keyReference }, (result) => {
                this.contentEditor.setValue(result.Content);
                this.keyEditor.setValue(result.Key);

                this.editingKeyType = keyType;
                this.editingContentType = contentType;
                this.editingKeyReference = keyReference;

                this.keyTypeBox.option("value", keyType);
                this.contentTypeBox.option("value", contentType);

                this.keyEditor.setOption("readOnly", true);
                this.contentEditor.setOption("readOnly", true);

                RIFFWebCore.RIFFPage.hideLoadPanel();
            });
        }

        deleteKey() {
            window.location.assign(this.options.urlInvalidateEntry + '?type=' + this.editingKeyType + '&keyReference=' + this.editingKeyReference);
        }

        viewKey() {
            window.location.assign(this.options.urlDataSet + '?type=' + this.editingKeyType + '&keyReference=' + this.editingKeyReference);
        }

        xmlKey() {
            window.location.assign(this.options.urlExportEntry + '?type=' + this.editingKeyType + '&keyReference=' + this.editingKeyReference);
        }

        excelKey() {
            window.location.assign(this.options.urlDownloadEntry + '?type=' + this.editingKeyType + '&keyReference=' + this.editingKeyReference);
        }

        update() {
            RIFFWebCore.Helpers.postUserAction(this.options.urlUpdateDocument, { type: this.editingKeyType, keyReference: this.editingKeyReference, data: this.contentEditor.getValue('') }, "Catalog updated.",
                this.resetGrid);
        }

        resetGrid() {
            this.refreshGrid(null /*this.getValueDate()*/);
            this.editRow(this.editingKeyType, this.editingContentType, this.editingKeyReference);
        }

        copy() {
            $('#updatebutton').hide();
            $('#editbutton').hide();
            $('#copybutton').hide();
            $('#savebutton').show();

            this.keyEditor.setOption("readOnly", false)
            this.contentEditor.setOption("readOnly", false)
        }

        edit() {
            $('#editbutton').hide();
            $('#updatebutton').show();

            this.contentEditor.setOption("readOnly", false)
        }

        _import() {
            $('#importentry').dxPopup({
                showTitle: true,
                title: 'Import XML',
                width: 600,
                height: 250,
                visible: true
            });

            $('#uploadfile').dxFileUploader({
                name: 'filedata', multiple: false, uploadMode: 'instantly', labelText: '(or drop here)',
                onUploaded: function () {
                    $('#uploadform').submit();
                }
            });
        }

        save() {
            RIFFWebCore.Helpers.postUserAction(this.options.urlSaveDocument, {
                keyType: this.editingKeyType, contentType: this.editingContentType, keyData: this.keyEditor.getValue(''),
                contentData: this.contentEditor.getValue('')
            }, "Catalog updated.",
                () => this.resetGrid);
        }

        /*
        // TODO: work in progress, OData turned out to be a little disappointing
        var store = new DevExpress.data.ODataStore({
            url: "/odata/RFCatalogKeyData",
            key: "KeyReference",
            keyType: "Int64",
            deserializeDates: true,
            version: 4
        });
        */

        trimType(s) {
            if (s.lastIndexOf('.') == -1) {
                return s;
            }
            return s.substring(s.lastIndexOf('.') + 1);
        }

        keyTypeValue(rowData: any) {
            return this.trimType(rowData.KeyTypeFull);
        }

        contentTypeValue(rowData: any) {
            return this.trimType(rowData.ContentTypeFull);
        }

        onRowClick(e: any) {
            if (e.rowType == 'data' && e.isSelected) {
                this.editRow(e.data.KeyTypeFull, e.data.ContentTypeFull, e.data.KeyReference);
            }
        };

        initializeGrid(data: any, options: DataEditorOptions) {
            $("#gridContainer").dxDataGrid({
                hoverStateEnabled: true,
                dataSource: data,

                columns: [
                    { dataField: 'Plane', caption: 'Plane', width: "75px", allowEditing: false, dataType: 'string', filterValues: ['User', 'System'], visible: false },
                    { calculateCellValue: (e) => this.keyTypeValue(e), caption: 'Key Type', width: "180px", allowEditing: false, dataType: 'string', groupIndex: 0, autoExpandGroup: true, sortIndex: 0, sortOrder: 'asc' },
                    { calculateCellValue: (e) => this.contentTypeValue(e), caption: 'Content Type', width: "200px", allowEditing: false, dataType: 'string', sortIndex: 1, sortOrder: 'asc' },
                    { dataField: 'FriendlyString', caption: 'Path', width: "260px", visible: true, allowEditing: false, dataType: 'string', sortIndex: 2, sortOrder: 'asc' },
                    { dataField: 'ValueDate', caption: 'Latest', dataType: 'date', format: "yyyy-MM-dd", width: "85px", visible: true, allowEditing: false, alignment: "center" },
                    { dataField: 'IsValid', caption: 'Valid', width: "65px", dataType: 'boolean', allowEditing: false, visible: false },
                    { dataField: 'DataSize', caption: 'Size', width: "70px", format: 'fixedPoint', allowEditing: true, dataType: 'number' },
                    { dataField: 'UpdateTime', caption: 'Update', dataType: 'date', format: "d MMM, HH:mm", width: "75px", visible: true, allowEditing: false, alignment: "right" },
                    { dataField: 'KeyReference', visible: false, allowEditing: false, dataType: 'number' },
                ],
                customizeColumns: (columns) => {
                    $.each(columns, function (_, element) {
                        element.groupCellTemplate = this.groupCellTemplate;
                    });
                },
                columnChooser: { enabled: true },
                allowColumnReordering: true,
                allowColumnResizing: true,
                grouping: {
                    autoExpandAll: false
                },
                onCellPrepared: (e: any) => {
                    if (e !== undefined && e.rowType === 'data' && e.column.caption === 'Latest') {
                        if (!e.data.IsLatest) {
                            e.cellElement.css('color', '#777');
                        }
                    }
                },
                masterDetail: {
                    enabled: true,
                    template: (container, info) => {
                        $.get(options.urlGetInstances, { keyType: info.row.data.KeyTypeFull, keyReference: info.row.data.KeyReference }, function (instances) {
                            $('<div>').dxDataGrid({
                                hoverStateEnabled: true,
                                columnAutoWidth: true,
                                dataSource: instances,
                                columns: [
                                    { dataField: 'VD', caption: 'Date', dataType: 'date', format: "yyyy-MM-dd", width: "100px", visible: true, allowEditing: false, alignment: "center", sortIndex: 0, sortOrder: 'desc' },
                                    { dataField: 'DS', caption: 'Size', width: "100px", format: 'fixedPoint', allowEditing: true, dataType: 'number' },
                                    { dataField: 'UT', caption: 'Update', dataType: 'date', format: "d MMM, HH:mm", width: "100px", visible: true, allowEditing: false, alignment: "right" },
                                ],
                                pager: { visible: false },
                                paging: { enabled: false },
                                headerFilter: { visible: true },
                                rowAlternationEnabled: true,
                                selection: { mode: 'single' },
                                scrolling: {
                                    mode: 'virtual',
                                    preloadEnabled: true
                                },
                                onRowClick: (e) => {
                                    if (e.rowType == 'data' && e.isSelected) {
                                        (codeBehind as RIFF.Web.System.DataEditorPage).editRow(e.component.element().attr('data-keytype'), e.component.element().attr('data-contenttype'), e.data.KR);
                                    }
                                }
                            }).attr('data-keytype', info.data.KeyTypeFull).attr('data-contenttype', info.data.ContentTypeFull).appendTo(container);
                        });
                    }
                },
                onRowClick: (e) => this.onRowClick(e),
                sorting: { mode: 'single' },
                groupPanel: { visible: true, emptyPanelText: 'Drag a column header here to group grid records' },
                columnAutoWidth: false,
                pager: { visible: false },
                paging: { enabled: false },
                headerFilter: { visible: true },
                scrolling: {
                    mode: 'virtual',
                    preloadEnabled: true
                },
                rowAlternationEnabled: true,
                loadPanel: {
                    enabled: false
                },
                filterRow: { visible: false },
                searchPanel: { visible: true },
                selection: { mode: 'single' }
            });

            $('#gridContainer').css('height', ($(window).height() - 120) + 'px');
            $('#gridContainer').dxDataGrid('instance').repaint()

            $('#editingPanel').css('width', ($(window).width() - 830) + 'px');
            //this.contentEditor.setSize(null, $(window).height() - 377);
        }

        getValueDate() {
            return RIFFWebCore.Helpers.getYMD($('#datebox').dxDateBox('instance').option("value"));
        }

        refreshGrid(valueDate) {
            RIFFWebCore.RIFFPage.populateGrid(this.options.urlGetLatestDocuments, { valueDate: valueDate }, (data) => this.initializeGrid(data, this.options));
        }
    }
}
