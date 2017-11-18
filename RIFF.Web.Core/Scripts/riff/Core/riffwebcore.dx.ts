/// <reference path="../../typings/jquery/jquery.d.ts" />
/// <reference path="../../typings/devextreme/devextreme.d.ts" />

module RIFFWebCore {
    export module dx {
        export function numberWithCommas(value: any, digits: number) {
            if (value) {
                return value.toFixed(digits).toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
            }
            return '';
        }

        export function removeInputCommas(t: any) {
            var v = $(t).val();
            if (v && v.indexOf(',') > -1) {
                $(t).val(v.replace(/,/g, ''));
            }
        }

        // supply as editorOptions to numeric dxDataGrid columns in order to support comma grouping input with two decimals
        // i.e. editorOptions: RIFFWebCore.dx.numericEditorOptions()
        export function numericEditorOptions()
        {
            return { inputAttr: { oninput: "RIFFWebCore.dx.removeInputCommas(this);" }, valueFormat: function (v) { return RIFFWebCore.dx.numberWithCommas(parseFloat(v), 2); } };
        }
    }
}
