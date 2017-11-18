var __extends = (this && this.__extends) || (function () {
    var extendStatics = Object.setPrototypeOf ||
        ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
        function (d, b) { for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p]; };
    return function (d, b) {
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
var RIFF;
(function (RIFF) {
    var Web;
    (function (Web) {
        var Core;
        (function (Core) {
            var Control = /** @class */ (function () {
                function Control(owner, options) {
                    var _this = this;
                    this.owner = owner;
                    this.options = options;
                    this.defaultOnInitialized = function (e) {
                        _this.control = e.component;
                        if (_this.customOnInitialized) {
                            _this.customOnInitialized();
                        }
                    };
                    this.options = $.extend(this.defaults(), options);
                    // keep the special init if present
                    this.customOnInitialized = this.options.onInitialized;
                    this.options.onInitialized = this.defaultOnInitialized;
                    if (owner) {
                        owner.registerControl(this);
                    }
                }
                Control.prototype.repaint = function () {
                    if (this.control) {
                        this.control.repaint();
                    }
                };
                Control.prototype.extend = function (options) {
                    if (options.onInitialized) {
                        this.customOnInitialized = options.onInitialized;
                    }
                    this.options = $.extend(this.options, options);
                    this.options.onInitialized = this.defaultOnInitialized;
                    return this;
                };
                return Control;
            }());
            Core.Control = Control;
            var DataGrid = /** @class */ (function (_super) {
                __extends(DataGrid, _super);
                function DataGrid() {
                    return _super !== null && _super.apply(this, arguments) || this;
                }
                DataGrid.prototype.defaults = function () {
                    return {
                        activeStateEnabled: true,
                        hoverStateEnabled: true,
                        rowAlternationEnabled: true,
                        pager: { visible: false },
                        paging: { enabled: false }
                    };
                };
                return DataGrid;
            }(Control));
            Core.DataGrid = DataGrid;
            var TreeView = /** @class */ (function (_super) {
                __extends(TreeView, _super);
                function TreeView() {
                    return _super !== null && _super.apply(this, arguments) || this;
                }
                TreeView.prototype.defaults = function () {
                    return {
                        activeStateEnabled: true,
                        hoverStateEnabled: true,
                    };
                };
                return TreeView;
            }(Control));
            Core.TreeView = TreeView;
            var SelectBox = /** @class */ (function (_super) {
                __extends(SelectBox, _super);
                function SelectBox() {
                    return _super !== null && _super.apply(this, arguments) || this;
                }
                SelectBox.prototype.defaults = function () {
                    return {
                        activeStateEnabled: true,
                        hoverStateEnabled: true,
                    };
                };
                return SelectBox;
            }(Control));
            Core.SelectBox = SelectBox;
        })(Core = Web.Core || (Web.Core = {}));
    })(Web = RIFF.Web || (RIFF.Web = {}));
})(RIFF || (RIFF = {}));
//# sourceMappingURL=Controls.js.map