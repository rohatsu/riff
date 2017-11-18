/// <reference path="../Core/PageBase.ts" />

namespace RIFF.Web.System {
    interface MirrorOptions {
        urlGetFiles: string,
        urlGetFile: string,
        urlGetPreview: string,
        dataSources: any[]
    }

    export class MirrorPage extends RIFF.Web.Core.PageBase {

        public sourcesGrid = new RIFF.Web.Core.TreeView(this, {
            displayExpr: 'name',
            selectByClick: true,
            selectionMode: 'single',
            onItemSelectionChanged: (e: any) => {
                RIFFWebCore.RIFFPage.populateGrid(this.options.urlGetFiles, { name: e.node.itemData.name, type: e.node.itemData.type }, (data) => {
                    this.filesGrid.control.option('dataSource', data.files);
                });
            }
        });

        public filesGrid = new RIFF.Web.Core.DataGrid(this, {
            columns: [
                { dataField: 'ReceivedTime', caption: 'Received', width: '130px', alignment: 'center', dataType: 'date', format: 'yyyy-MM-dd HH:mm:ss', sortIndex: 0, sortOrder: 'desc' },
                { dataField: 'FileName', caption: 'File Name', width: '700px', dataType: 'string' },
                { dataField: 'FileSize', caption: 'File Size', width: '80px', dataType: 'number', format: { type: 'fixedPoint' } },
                { dataField: 'ModifiedTime', caption: 'Modified', width: '130px', alignment: 'center', dataType: 'date', format: 'yyyy-MM-dd HH:mm:ss' },
                { dataField: 'SourceSite', caption: 'Source Site', width: '150px', dataType: 'string' },
                {
                    dataField: 'MirroredFileID',
                    caption: 'Download',
                    width: '100px',
                    alignment: 'center',
                    cellTemplate: (container, options) => {
                        $('<a />').addClass('dx-link')
                            .text('Download')
                            .attr('href', this.options.urlGetFile + '?mirroredFileID=' + options.data.MirroredFileID)
                            .appendTo(container);
                    },
                },
                { dataField: 'SourcePath', caption: 'Source Path', width: '500px', dataType: 'string', visible: false }
            ],
            columnChooser: { enabled: true },
            searchPanel: { visible: true },
            selection: { mode: 'single' },
            onSelectionChanged: (e: any) => {
                if (e.selectedRowsData && e.selectedRowsData.length > 0) {
                    this.selectedMirroredFileID = e.selectedRowsData[0].MirroredFileID;
                    this.selectedSectionNo = 0;
                    this.refreshPreview();
                }
                else {
                    this.clearPreview();
                }
            }
        });

        private selectedMirroredFileID: number = undefined;
        private selectedSectionNo: number = undefined;

        public previewGrid = new RIFF.Web.Core.DataGrid(this, {
            columnMinWidth: 80,
            columnAutoWidth: true,
            scrolling: {
                useNative: true
            }
        });

        public refreshPreview() {
            if (this.selectedMirroredFileID !== undefined && this.selectedSectionNo !== undefined) {
                RIFFWebCore.RIFFPage.populateGrid(this.options.urlGetPreview, { mirroredFileID: this.selectedMirroredFileID, sectionNo: this.selectedSectionNo }, (data) => {
                    this.previewGrid.control.option('dataSource', data.preview);
                    this.sectionSelect.control.option('dataSource', data.sections);
                    this.sectionSelect.control.option('value', data.selectedSection);
                });
            }
        }

        public clearPreview() {
            this.selectedMirroredFileID = undefined;
            this.selectedSectionNo = undefined;

            this.previewGrid.control.option('dataSource', []);
            this.sectionSelect.control.option('dataSource', []);
        }

        public sectionSelect = new RIFF.Web.Core.SelectBox(this, {
            valueExpr: 'id',
            displayExpr: 'name',
            onValueChanged: (e: any) => {
                if (e.value != this.selectedSectionNo) {
                    this.selectedSectionNo = e.value;
                    this.refreshPreview();
                }
            }
        });

        constructor(private options: MirrorOptions) {
            super();

            this.sourcesGrid.extend({ dataSource: options.dataSources });
        }

        onInitialize() {
            ko.applyBindings(this);
        }

        onResized() {
        }

        onInitialized() {
        }
    }
}
