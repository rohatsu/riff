namespace RIFF.Web.Core {

    export interface IControl {
        repaint();
    }

    export abstract class Control<C extends DevExpress.ui.Widget, O extends DevExpress.ComponentOptions> implements IControl {
        public control: C;

        private customOnInitialized: Function;

        constructor(private owner: PageBase, public options: O) {
            this.options = $.extend(this.defaults(), options);

            // keep the special init if present
            this.customOnInitialized = this.options.onInitialized;
            this.options.onInitialized = this.defaultOnInitialized;

            if (owner) {
                owner.registerControl(this);
            }
        }

        abstract defaults(): DevExpress.ComponentOptions;

        repaint() {
            if (this.control) {
                this.control.repaint();
            }
        }

        extend(options: O) : Control<C, O> {
            if (options.onInitialized)
            {
                this.customOnInitialized = options.onInitialized;
            }
            this.options = $.extend(this.options, options);
            this.options.onInitialized = this.defaultOnInitialized;
            return this;
        }

        defaultOnInitialized = (e: any) => {
            this.control = e.component;
            if (this.customOnInitialized) {
                this.customOnInitialized();
            }
        };
    }

    export class DataGrid extends Control<DevExpress.ui.dxDataGrid, DevExpress.ui.dxDataGridOptions>
    {
        defaults(): DevExpress.ui.dxDataGridOptions {
            return {
                activeStateEnabled: true,
                hoverStateEnabled: true,
                rowAlternationEnabled: true,
                pager: { visible: false },
                paging: { enabled: false }
            }
        }
    }

    export class TreeView extends Control<DevExpress.ui.dxTreeView, DevExpress.ui.dxTreeViewOptions>
    {
        defaults(): DevExpress.ui.dxTreeViewOptions {
            return {
                activeStateEnabled: true,
                hoverStateEnabled: true,
            }
        }
    }

    export class SelectBox extends Control<DevExpress.ui.dxSelectBox, DevExpress.ui.dxSelectBoxOptions>
    {
        defaults(): DevExpress.ui.dxSelectBoxOptions {
            return {
                activeStateEnabled: true,
                hoverStateEnabled: true,
            }
        }
    }
}
