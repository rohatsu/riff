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
            var DateBox = /** @class */ (function (_super) {
                __extends(DateBox, _super);
                function DateBox() {
                    return _super !== null && _super.apply(this, arguments) || this;
                }
                DateBox.prototype.defaults = function () {
                    return {
                        activeStateEnabled: true,
                        hoverStateEnabled: true,
                    };
                };
                return DateBox;
            }(Control));
            Core.DateBox = DateBox;
        })(Core = Web.Core || (Web.Core = {}));
    })(Web = RIFF.Web || (RIFF.Web = {}));
})(RIFF || (RIFF = {}));
/// <reference path="../../typings/knockout/knockout.d.ts" />
/// <reference path="../../typings/knockout.mapping/knockout.mapping.d.ts" />
/// <reference path="Controls.ts" />
var RIFF;
(function (RIFF) {
    var Web;
    (function (Web) {
        var Core;
        (function (Core) {
            var PageBase = /** @class */ (function () {
                function PageBase() {
                    var _this = this;
                    this.controls = [];
                    this.resizeHandler = function () {
                        $('body').height(window.document.documentElement.clientHeight);
                        if (_this.controls.length > 0) {
                            _this.controls.forEach(function (c) { return c.repaint(); });
                        }
                        _this.onResized();
                    };
                }
                PageBase.prototype.registerControl = function (c) {
                    this.controls.push(c);
                };
                PageBase.prototype.start = function () {
                    var _this = this;
                    $.ajaxSetup({
                        cache: false,
                        "error": function (jqXHR, textStatus, errorThrown) {
                            RIFFWebCore.RIFFPage.hideLoadPanel();
                            RIFFWebCore.RIFFPage.hideProcessingPanel();
                            if (errorThrown && errorThrown.toString().indexOf("There was no endpoint") >= 0) {
                                errorThrown = "System is offline";
                            }
                            if (textStatus && textStatus.toString().indexOf("There was no endpoint") >= 0) {
                                textStatus = "System is offline";
                            }
                            DevExpress.ui.notify("Error " + textStatus + ": " + errorThrown, "error", 100000);
                        }
                    });
                    var page = this;
                    window.onerror = function (msg, url, line, col, error) {
                        if (!page.onError(msg)) {
                            DevExpress.ui.notify("Unexpected error, please contact IT: " + msg, "error", 5000);
                            page.hideLoadPanel();
                            page.hideProcessingPanel();
                            page.hideWorkIndicator();
                        }
                    };
                    this.onInitialize();
                    this.resizeHandler();
                    $('#riffbodywrapper').css('visibility', 'visible');
                    this.onInitialized();
                    $(window).resize(function () {
                        clearTimeout(_this.resizeHandle);
                        _this.resizeHandle = setTimeout(_this.resizeHandler, 250);
                    });
                };
                // browser resized
                PageBase.prototype.onResized = function () { };
                // error thrown
                PageBase.prototype.onError = function (msg) {
                    return false;
                };
                PageBase.prototype.populateGrid = function (url, params, callback) {
                    RIFFWebCore.RIFFPage.populateGrid(url, params, callback);
                };
                PageBase.prototype.showProcessingPanel = function () {
                    RIFFWebCore.RIFFPage.showProcessingPanel();
                };
                PageBase.prototype.hideProcessingPanel = function () {
                    RIFFWebCore.RIFFPage.hideProcessingPanel();
                };
                PageBase.prototype.showLoadPanel = function () {
                    RIFFWebCore.RIFFPage.showLoadPanel();
                };
                PageBase.prototype.hideLoadPanel = function () {
                    RIFFWebCore.RIFFPage.hideLoadPanel();
                };
                PageBase.prototype.showWorkIndicator = function () {
                    RIFFWebCore.RIFFPage.showWorkIndicator();
                };
                PageBase.prototype.hideWorkIndicator = function () {
                    RIFFWebCore.RIFFPage.hideWorkIndicator();
                };
                return PageBase;
            }());
            Core.PageBase = PageBase;
            var LegacyPage = /** @class */ (function (_super) {
                __extends(LegacyPage, _super);
                function LegacyPage() {
                    return _super !== null && _super.apply(this, arguments) || this;
                }
                LegacyPage.prototype.onInitialize = function () {
                    if (typeof (RFinitialize) == "function") {
                        RFinitialize();
                    }
                };
                LegacyPage.prototype.onInitialized = function () {
                    if (typeof (RFfinalize) == "function") {
                        RFfinalize();
                    }
                };
                LegacyPage.prototype.onResize = function () {
                    if (typeof (RFresize) == "function") {
                        RFresize();
                    }
                };
                return LegacyPage;
            }(PageBase));
            Core.LegacyPage = LegacyPage;
        })(Core = Web.Core || (Web.Core = {}));
    })(Web = RIFF.Web || (RIFF.Web = {}));
})(RIFF || (RIFF = {}));
/// <reference path="../Core/PageBase.ts" />
var codeBehind;
var Globalize;
var RIFF;
(function (RIFF) {
    var Web;
    (function (Web) {
        var Core;
        (function (Core) {
            var LayoutBase = /** @class */ (function () {
                function LayoutBase(options) {
                    this.options = options;
                }
                LayoutBase.prototype.togglePresentationMode = function () {
                    this.options.isPresentationMode = !this.options.isPresentationMode;
                    $('#presentationmode').css('cursor', 'wait');
                    RIFFWebCore.RIFFPage.showWorkIndicator();
                    $.post(this.options.urlSetPresentationMode, { active: this.options.isPresentationMode }, function (data) {
                        window.location.reload();
                    });
                };
                LayoutBase.prototype.refreshPresentationMode = function () {
                    if (this.options.isPresentationMode) {
                        $('#presentationmode').css('color', '#000');
                        $('#presentationmode').attr('title', 'PRESENTATION MODE ACTIVE');
                    }
                    else {
                        $('#presentationmode').css('color', '#abc');
                        $('#presentationmode').attr('title', 'Click to enable Presentation Mode');
                    }
                };
                LayoutBase.prototype.refreshSystemStatus = function () {
                    $.get(this.options.urlSystemStatus, function (data) {
                        if (data) {
                            if (data.Status == "OK") {
                                $('#statusindicator_err').hide();
                                $('#statusindicator_warn').hide();
                                $('#statusindicator_ok').show();
                                $('#statusindicator_ok').attr('title', data.Message);
                            }
                            else if (data.Status == "Warning") {
                                $('#statusindicator_ok').hide();
                                $('#statusindicator_err').hide();
                                $('#statusindicator_warn').attr('title', data.Message);
                                $('#statusindicator_warn').show();
                            }
                            else if (data.Status == "Error") {
                                $('#statusindicator_ok').hide();
                                $('#statusindicator_warn').hide();
                                $('#statusindicator_err').attr('title', data.Message);
                                $('#statusindicator_err').show();
                            }
                        }
                    });
                };
                LayoutBase.prototype.start = function () {
                    var _this = this;
                    this.refreshPresentationMode();
                    Globalize.load({
                        "main": {
                            "en-HK": {
                                "identity": {
                                    "version": {
                                        "_number": "$Revision: 11914 $",
                                        "_cldrVersion": "29"
                                    },
                                    "language": "en",
                                    "territory": "HK"
                                },
                                "numbers": {
                                    "defaultNumberingSystem": "latn",
                                    "otherNumberingSystems": {
                                        "native": "latn"
                                    },
                                    "minimumGroupingDigits": "1",
                                    "symbols-numberSystem-latn": {
                                        "decimal": ".",
                                        "group": ",",
                                        "list": ";",
                                        "percentSign": "%",
                                        "plusSign": "+",
                                        "minusSign": "-",
                                        "exponential": "E",
                                        "superscriptingExponent": "×",
                                        "perMille": "‰",
                                        "infinity": "∞",
                                        "nan": "NaN",
                                        "timeSeparator": ":"
                                    },
                                    "decimalFormats-numberSystem-latn": {
                                        "standard": "#,##0.###",
                                        "long": {
                                            "decimalFormat": {
                                                "1000-count-one": "0 thousand",
                                                "1000-count-other": "0 thousand",
                                                "10000-count-one": "00 thousand",
                                                "10000-count-other": "00 thousand",
                                                "100000-count-one": "000 thousand",
                                                "100000-count-other": "000 thousand",
                                                "1000000-count-one": "0 million",
                                                "1000000-count-other": "0 million",
                                                "10000000-count-one": "00 million",
                                                "10000000-count-other": "00 million",
                                                "100000000-count-one": "000 million",
                                                "100000000-count-other": "000 million",
                                                "1000000000-count-one": "0 billion",
                                                "1000000000-count-other": "0 billion",
                                                "10000000000-count-one": "00 billion",
                                                "10000000000-count-other": "00 billion",
                                                "100000000000-count-one": "000 billion",
                                                "100000000000-count-other": "000 billion",
                                                "1000000000000-count-one": "0 trillion",
                                                "1000000000000-count-other": "0 trillion",
                                                "10000000000000-count-one": "00 trillion",
                                                "10000000000000-count-other": "00 trillion",
                                                "100000000000000-count-one": "000 trillion",
                                                "100000000000000-count-other": "000 trillion"
                                            }
                                        },
                                        "short": {
                                            "decimalFormat": {
                                                "1000-count-one": "0K",
                                                "1000-count-other": "0K",
                                                "10000-count-one": "00K",
                                                "10000-count-other": "00K",
                                                "100000-count-one": "000K",
                                                "100000-count-other": "000K",
                                                "1000000-count-one": "0M",
                                                "1000000-count-other": "0M",
                                                "10000000-count-one": "00M",
                                                "10000000-count-other": "00M",
                                                "100000000-count-one": "000M",
                                                "100000000-count-other": "000M",
                                                "1000000000-count-one": "0B",
                                                "1000000000-count-other": "0B",
                                                "10000000000-count-one": "00B",
                                                "10000000000-count-other": "00B",
                                                "100000000000-count-one": "000B",
                                                "100000000000-count-other": "000B",
                                                "1000000000000-count-one": "0T",
                                                "1000000000000-count-other": "0T",
                                                "10000000000000-count-one": "00T",
                                                "10000000000000-count-other": "00T",
                                                "100000000000000-count-one": "000T",
                                                "100000000000000-count-other": "000T"
                                            }
                                        }
                                    },
                                    "scientificFormats-numberSystem-latn": {
                                        "standard": "#E0"
                                    },
                                    "percentFormats-numberSystem-latn": {
                                        "standard": "#,##0%"
                                    },
                                    "currencyFormats-numberSystem-latn": {
                                        "currencySpacing": {
                                            "beforeCurrency": {
                                                "currencyMatch": "[:^S:]",
                                                "surroundingMatch": "[:digit:]",
                                                "insertBetween": " "
                                            },
                                            "afterCurrency": {
                                                "currencyMatch": "[:^S:]",
                                                "surroundingMatch": "[:digit:]",
                                                "insertBetween": " "
                                            }
                                        },
                                        "standard": "¤#,##0.00",
                                        "accounting": "¤#,##0.00;(¤#,##0.00)",
                                        "short": {
                                            "standard": {
                                                "1000-count-one": "¤0K",
                                                "1000-count-other": "¤0K",
                                                "10000-count-one": "¤00K",
                                                "10000-count-other": "¤00K",
                                                "100000-count-one": "¤000K",
                                                "100000-count-other": "¤000K",
                                                "1000000-count-one": "¤0M",
                                                "1000000-count-other": "¤0M",
                                                "10000000-count-one": "¤00M",
                                                "10000000-count-other": "¤00M",
                                                "100000000-count-one": "¤000M",
                                                "100000000-count-other": "¤000M",
                                                "1000000000-count-one": "¤0B",
                                                "1000000000-count-other": "¤0B",
                                                "10000000000-count-one": "¤00B",
                                                "10000000000-count-other": "¤00B",
                                                "100000000000-count-one": "¤000B",
                                                "100000000000-count-other": "¤000B",
                                                "1000000000000-count-one": "¤0T",
                                                "1000000000000-count-other": "¤0T",
                                                "10000000000000-count-one": "¤00T",
                                                "10000000000000-count-other": "¤00T",
                                                "100000000000000-count-one": "¤000T",
                                                "100000000000000-count-other": "¤000T"
                                            }
                                        },
                                        "unitPattern-count-one": "{0} {1}",
                                        "unitPattern-count-other": "{0} {1}"
                                    },
                                    "miscPatterns-numberSystem-latn": {
                                        "atLeast": "{0}+",
                                        "range": "{0}–{1}"
                                    },
                                }
                            }
                        },
                        supplemental: {
                            weekData: {
                                firstDay: { "HK": "mon" }
                            }
                        }
                    });
                    Globalize.locale('en-HK');
                    if (!this.options.suppressMenu) {
                        $("#riffMenu").dxMenu({
                            items: this.options.menuItems,
                            orientation: 'horizontal',
                            showFirstSubmenuMode: {
                                name: "onHover",
                                delay: { show: 0, hide: 0 }
                            },
                            showSubmenuMode: {
                                name: "onHover",
                                delay: { show: 0, hide: 0 }
                            },
                            hoverStateEnabled: true,
                            hideSubmenuOnMouseLeave: false,
                            cssClass: "menucss",
                            onItemClick: function (data) {
                                if (data.itemData.url != null) {
                                    if (data.itemData.newTab) {
                                        window.open(data.itemData.url, '_blank');
                                    }
                                    else {
                                        window.location.assign(data.itemData.url);
                                    }
                                }
                            },
                        });
                    }
                    $('#workindicator').dxLoadIndicator({
                        visible: false,
                        height: '22px'
                    });
                    if (this.options.urlHelp) {
                        $("#riffHelp").dxMenu({
                            items: [{
                                    text: 'Help', disabled: false,
                                    icon: 'help',
                                    url: this.options.urlHelp,
                                    newTab: true
                                }],
                            hoverStateEnabled: true,
                            hideSubmenuOnMouseLeave: false,
                            cssClass: "menucss",
                            onItemClick: function (data) {
                                if (data.itemData.url != null) {
                                    if (data.itemData.newTab) {
                                        window.open(data.itemData.url, '_blank');
                                    }
                                    else {
                                        window.location.assign(data.itemData.url);
                                    }
                                }
                            },
                        });
                    }
                    this.refreshSystemStatus();
                    $('#presentationmode').click(function () { return _this.togglePresentationMode(); });
                    if (typeof codeBehind === 'undefined') {
                        codeBehind = new RIFF.Web.Core.LegacyPage();
                    }
                    codeBehind.start();
                };
                return LayoutBase;
            }());
            Core.LayoutBase = LayoutBase;
        })(Core = Web.Core || (Web.Core = {}));
    })(Web = RIFF.Web || (RIFF.Web = {}));
})(RIFF || (RIFF = {}));
/// <reference path="../../typings/jquery/jquery.d.ts" />
/// <reference path="../../typings/devextreme/devextreme.d.ts" />
var RIFFWebCore;
(function (RIFFWebCore) {
    var dx;
    (function (dx) {
        function numberWithCommas(value, digits) {
            if (value) {
                return value.toFixed(digits).toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
            }
            return '';
        }
        dx.numberWithCommas = numberWithCommas;
        function removeInputCommas(t) {
            var v = $(t).val();
            if (v && v.indexOf(',') > -1) {
                $(t).val(v.replace(/,/g, ''));
            }
        }
        dx.removeInputCommas = removeInputCommas;
        // supply as editorOptions to numeric dxDataGrid columns in order to support comma grouping input with two decimals
        // i.e. editorOptions: RIFFWebCore.dx.numericEditorOptions()
        function numericEditorOptions() {
            return { inputAttr: { oninput: "RIFFWebCore.dx.removeInputCommas(this);" }, valueFormat: function (v) { return RIFFWebCore.dx.numberWithCommas(parseFloat(v), 2); } };
        }
        dx.numericEditorOptions = numericEditorOptions;
    })(dx = RIFFWebCore.dx || (RIFFWebCore.dx = {}));
})(RIFFWebCore || (RIFFWebCore = {}));
/// <reference path="../../typings/jquery/jquery.d.ts" />
/// <reference path="../../typings/devextreme/devextreme.d.ts" />
var RIFFWebCore;
(function (RIFFWebCore) {
    var Helpers;
    (function (Helpers) {
        // returns promise whether to cancel or not
        function postUserAction(url, data, successString, successCallback, errorCallback, alwaysCallback) {
            if (successCallback === void 0) { successCallback = null; }
            if (errorCallback === void 0) { errorCallback = null; }
            if (alwaysCallback === void 0) { alwaysCallback = null; }
            var dfd = $.Deferred();
            RIFFWebCore.RIFFPage.showWorkIndicator();
            dfd.always(function () {
                RIFFWebCore.RIFFPage.hideWorkIndicator();
                if (alwaysCallback) {
                    alwaysCallback();
                }
            });
            var p = $.post(url, data).done(function (response) {
                if (response.ErrorMessage) {
                    if (response.ErrorMessage.indexOf("There was no endpoint") >= 0) {
                        response.ErrorMessage = "System is offline";
                    }
                    dfd.resolve(true);
                    DevExpress.ui.notify("Error: " + response.ErrorMessage, "error", 10000);
                    if (errorCallback) {
                        errorCallback();
                    }
                }
                else if (response.RedirectUrl) {
                    window.location.assign(response.RedirectUrl);
                }
                else {
                    dfd.resolve(false);
                    if (successString) {
                        DevExpress.ui.notify(successString, "success", 2000);
                    }
                    if (successCallback) {
                        successCallback(response);
                    }
                }
            }).fail(function () {
                dfd.resolve(true);
            });
            return dfd.promise();
        }
        Helpers.postUserAction = postUserAction;
        function postUserActionProcessing(url, data, successString, callback) {
            RIFFWebCore.RIFFPage.showProcessingPanel();
            $.post(url, data).done(function (response) {
                if (response.ErrorMessage) {
                    if (response.ErrorMessage.indexOf("There was no endpoint") >= 0) {
                        response.ErrorMessage = "System is offline";
                    }
                    RIFFWebCore.RIFFPage.hideProcessingPanel();
                    DevExpress.ui.notify("Error: " + response.ErrorMessage, "error", 10000);
                }
                else if (response.RedirectUrl) {
                    window.location.assign(response.RedirectUrl);
                }
                else if (successString !== undefined) {
                    RIFFWebCore.RIFFPage.hideProcessingPanel();
                    DevExpress.ui.notify(successString, "success", 2000);
                    if (callback !== undefined) {
                        callback();
                    }
                }
            });
        }
        Helpers.postUserActionProcessing = postUserActionProcessing;
        function postUserActionCallback(url, data, callback) {
            RIFFWebCore.RIFFPage.showProcessingPanel();
            $.post(url, data).done(function (response) {
                if (response.ErrorMessage) {
                    if (response.ErrorMessage.indexOf("There was no endpoint") >= 0) {
                        response.ErrorMessage = "System is offline";
                    }
                    RIFFWebCore.RIFFPage.hideProcessingPanel();
                    DevExpress.ui.notify("Error: " + response.ErrorMessage, "error", 10000);
                }
                else if (response.RedirectUrl) {
                    window.location.assign(response.RedirectUrl);
                }
                else {
                    RIFFWebCore.RIFFPage.hideProcessingPanel();
                    callback(response);
                }
            });
        }
        Helpers.postUserActionCallback = postUserActionCallback;
        function areYouSure(title, callback) {
            var result = DevExpress.ui.dialog.confirm("<p style='padding: 10px 20px 20px 5px;'>Are you sure you'd like to " + title + "?", 'Confirmation');
            result.done(function (dialogResult) {
                if (dialogResult) {
                    callback();
                }
            });
        }
        Helpers.areYouSure = areYouSure;
        function alert(message) {
            var result = DevExpress.ui.dialog.alert("<p style='padding: 10px 20px 20px 5px;'>" + message + "</p>", 'Alert');
        }
        Helpers.alert = alert;
        function message(message, callback) {
            var result = DevExpress.ui.dialog.alert("<p style='padding: 10px 20px 20px 5px;'>" + message + "</p>", 'Message');
            result.done(function (dialogResult) {
                if (dialogResult && callback) {
                    callback();
                }
            });
        }
        Helpers.message = message;
        function getYMD(date) {
            var dd = date.getDate();
            var mm = date.getMonth() + 1; //January is 0!
            var yyyy = date.getFullYear();
            if (dd < 10) {
                dd = '0' + dd;
            }
            if (mm < 10) {
                mm = '0' + mm;
            }
            return yyyy + '-' + mm + '-' + dd;
        }
        Helpers.getYMD = getYMD;
        function numberWithCommas(value, digits) {
            if (value || value == 0) {
                return value.toFixed(digits).toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
            }
            return '';
        }
        Helpers.numberWithCommas = numberWithCommas;
    })(Helpers = RIFFWebCore.Helpers || (RIFFWebCore.Helpers = {}));
})(RIFFWebCore || (RIFFWebCore = {}));
/// <reference path="../../typings/jquery/jquery.d.ts" />
/// <reference path="../../typings/devextreme/devextreme.d.ts" />
var RFinitialize;
var RFfinalize;
var RFresize;
var RFlayout;
var RIFFWebCore;
(function (RIFFWebCore) {
    var RIFFPage;
    (function (RIFFPage) {
        var isLoading = false;
        var isProcessing = false;
        function initialize() {
            $.ajaxSetup({
                cache: false,
                "error": function (jqXHR, textStatus, errorThrown) {
                    hideLoadPanel();
                    hideProcessingPanel();
                    if (errorThrown && errorThrown.indexOf("There was no endpoint") >= 0) {
                        errorThrown = "System is offline";
                    }
                    if (textStatus && textStatus.indexOf("There was no endpoint") >= 0) {
                        textStatus = "System is offline";
                    }
                    DevExpress.ui.notify("Error " + textStatus + ": " + errorThrown, "error", 100000);
                }
            });
            var page = this;
            window.onerror = function (msg, url, line, col, error) {
                DevExpress.ui.notify("Unexpected error, please contact IT: " + msg, "error", 5000);
                page.hideLoadPanel();
                page.hideProcessingPanel();
                page.hideWorkIndicator();
            };
            if (typeof (RFinitialize) == "function") {
                RFinitialize();
            }
            $('#riffbodywrapper').css('visibility', 'visible');
            if (typeof (RFfinalize) == "function") {
                RFfinalize();
            }
        }
        RIFFPage.initialize = initialize;
        function populateGrid(url, params, callback) {
            showLoadPanel();
            $.getJSON(url, params, function (result) {
                callback(result);
                hideLoadPanel();
            });
        }
        RIFFPage.populateGrid = populateGrid;
        function showProcessingPanel() {
            isProcessing = true;
            $('#loadPanel').dxLoadPanel({
                message: 'Processing...',
                visible: true,
                showIndicator: true
            });
        }
        RIFFPage.showProcessingPanel = showProcessingPanel;
        function hideProcessingPanel() {
            if (isProcessing) {
                $("#loadPanel").dxLoadPanel("instance").option("visible", false);
                isProcessing = false;
            }
        }
        RIFFPage.hideProcessingPanel = hideProcessingPanel;
        function showLoadPanel() {
            isLoading = true;
            $('#loadPanel').dxLoadPanel({
                message: 'Loading...',
                visible: true,
                showIndicator: true
            });
        }
        RIFFPage.showLoadPanel = showLoadPanel;
        function hideLoadPanel() {
            if (isLoading) {
                $("#loadPanel").dxLoadPanel("instance").option("visible", false);
                isLoading = false;
            }
        }
        RIFFPage.hideLoadPanel = hideLoadPanel;
        function showWorkIndicator() {
            $("#workindicator").dxLoadIndicator("instance").option("visible", true);
        }
        RIFFPage.showWorkIndicator = showWorkIndicator;
        function hideWorkIndicator() {
            $("#workindicator").dxLoadIndicator("instance").option("visible", false);
        }
        RIFFPage.hideWorkIndicator = hideWorkIndicator;
        function refreshSystemStatus() {
            RFlayout.refreshSystemStatus();
        }
        RIFFPage.refreshSystemStatus = refreshSystemStatus;
    })(RIFFPage = RIFFWebCore.RIFFPage || (RIFFWebCore.RIFFPage = {}));
})(RIFFWebCore || (RIFFWebCore = {}));
/// <reference path="../Core/PageBase.ts" />
var RIFF;
(function (RIFF) {
    var Web;
    (function (Web) {
        var System;
        (function (System) {
            var DataBrowserPage = /** @class */ (function (_super) {
                __extends(DataBrowserPage, _super);
                function DataBrowserPage(options) {
                    var _this = _super.call(this) || this;
                    _this.options = options;
                    _this.getColumnVisibility = function (breakdowns, includes) {
                        return this.currentSource.Columns
                            .map(function (c, i, a) {
                            return {
                                code: c.Code, visible: breakdowns.indexOf(c.Code) >= 0 || includes.indexOf(c.Code) >= 0 || c.Mandatory
                            };
                        });
                    };
                    _this.getDefaultWidth = function (c) {
                        switch (c.Format) {
                            case 'date':
                                return '120px';
                            case 'string':
                                return '175px';
                            case 'number':
                            default:
                                return '120px';
                        }
                    };
                    _this.getDefaultAlignment = function (c) {
                        switch (c.Format) {
                            case 'date':
                                return 'center';
                            default:
                                return null;
                        }
                    };
                    _this.getColumns = function (visible) {
                        var _this = this;
                        return this.currentSource.Columns
                            .map(function (c, i, a) {
                            return {
                                dataField: c.Code,
                                caption: visible ? c.Caption : ((c.Aggregator == "Sum" ? 'Σ ' : '') + c.Caption),
                                visible: visible,
                                width: visible ? null : _this.getDefaultWidth(c),
                                alignment: _this.getDefaultAlignment(c),
                                allowFiltering: c.Format == 'date',
                                allowHeaderFiltering: !c.Aggregator && c.Format != 'date',
                                format: c.Format == 'date' ? 'yyyy-MM-dd' : c.Format,
                                dataType: c.Format == 'date' ? 'date' : (c.Format == 'string' ? 'string' : 'number')
                            };
                        }).concat({ dataField: '__RowCount', visible: true, allowFiltering: false, allowHeaderFiltering: false, allowSorting: false, dataType: 'number', caption: '# Rows', alignment: 'right' });
                    };
                    _this.getSummary = function () {
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
                    };
                    _this.dataSource = new DevExpress.data.DataSource({
                        load: function (loadOptions) {
                            var d = $.Deferred();
                            if (!_this.currentSource) {
                                return null;
                            }
                            $.post(_this.options.urlGetAggregateQuery, { options: _this.queryOptions(loadOptions) }, function (data) {
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
                    return _this;
                }
                DataBrowserPage.prototype.onInitialize = function () {
                    var _this = this;
                    $('#sourcebox').dxSelectBox({
                        dataSource: this.options.dataSources,
                        displayExpr: 'Description',
                        onValueChanged: function (e) { return _this.switchSource(e.value); }
                    });
                };
                DataBrowserPage.prototype.queryOptions = function (loadOptions) {
                    return {
                        Breakdown: this.currentBreakdowns,
                        Data: this.currentIncludes,
                        Source: this.currentSource.Code,
                        //Group: (loadOptions && loadOptions.group) ? JSON.stringify(loadOptions.group) : "",
                        Filter: (loadOptions && loadOptions.filter) ? JSON.stringify(loadOptions.filter) : ""
                    };
                };
                DataBrowserPage.prototype.showSQL = function () {
                    DevExpress.ui.dialog.alert('<pre>' + $('#statusContainer').data('sql') + '</pre>', 'SQL View');
                };
                DataBrowserPage.prototype.switchSource = function (source) {
                    var _this = this;
                    $('#aggregateContainer').show();
                    $('#includeContainer').show();
                    this.currentSource = source;
                    var aggregators = source.Columns.filter(function (c, i, a) {
                        return c.CanGroup && !c.Mandatory;
                    });
                    var includers = source.Columns.filter(function (c, i, a) {
                        return c.Aggregator && !c.Mandatory;
                    });
                    $('#dataContainer').dxDataGrid({
                        dataSource: this.dataSource,
                        remoteOperations: {
                            filtering: true,
                            sorting: false,
                            paging: false,
                            grouping: false,
                            summary: false
                        },
                        onCellPrepared: function (e) {
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
                            template: function (container, info) {
                                var col = _this.getColumns(true);
                                $.post(_this.options.urlGetDetailQuery, { options: _this.queryOptions(null), group: JSON.stringify(info.row.data) }, function (result) {
                                    $('<div>').dxDataGrid({
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
                                        onCellPrepared: function (e) {
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
                    $('#aggregatebox').dxList({
                        dataSource: aggregators,
                        selectionMode: 'multiple',
                        showSelectionControls: true,
                        pageLoadMode: 'scrollBottom',
                        keyExpr: 'Code',
                        itemTemplate: function (itemData, itemIndex, itemElement) {
                            itemElement.append('<span title="' + (itemData.Description ? itemData.Description : itemData.Caption) + '">' + itemData.Caption + '</span>');
                        },
                        onSelectionChanged: function (data) { return _this.refreshData(); },
                    });
                    $('#includebox').dxList({
                        dataSource: includers,
                        selectionMode: 'multiple',
                        showSelectionControls: true,
                        pageLoadMode: 'scrollBottom',
                        keyExpr: 'Code',
                        itemTemplate: function (itemData, itemIndex, itemElement) {
                            itemElement.append('<span title="' + (itemData.Description ? itemData.Description : itemData.Caption) + '">' + itemData.Caption + '</span>');
                        },
                        onSelectionChanged: function (data) { return _this.refreshData(); }
                    });
                    this.refreshData();
                };
                DataBrowserPage.prototype.refreshData = function () {
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
                };
                DataBrowserPage.prototype.onInitialized = function () {
                };
                return DataBrowserPage;
            }(RIFF.Web.Core.PageBase));
            System.DataBrowserPage = DataBrowserPage;
        })(System = Web.System || (Web.System = {}));
    })(Web = RIFF.Web || (RIFF.Web = {}));
})(RIFF || (RIFF = {}));
/// <reference path="../Core/PageBase.ts" />
var RIFF;
(function (RIFF) {
    var Web;
    (function (Web) {
        var System;
        (function (System) {
            var MirrorPage = /** @class */ (function (_super) {
                __extends(MirrorPage, _super);
                function MirrorPage(options) {
                    var _this = _super.call(this) || this;
                    _this.options = options;
                    _this.sourcesGrid = new RIFF.Web.Core.TreeView(_this, {
                        displayExpr: 'name',
                        selectByClick: true,
                        selectionMode: 'single',
                        onItemSelectionChanged: function (e) {
                            RIFFWebCore.RIFFPage.populateGrid(_this.options.urlGetFiles, { name: e.node.itemData.name, type: e.node.itemData.type }, function (data) {
                                _this.filesGrid.control.option('dataSource', data.files);
                            });
                        }
                    });
                    _this.filesGrid = new RIFF.Web.Core.DataGrid(_this, {
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
                                cellTemplate: function (container, options) {
                                    $('<a />').addClass('dx-link')
                                        .text('Download')
                                        .attr('href', _this.options.urlGetFile + '?mirroredFileID=' + options.data.MirroredFileID)
                                        .appendTo(container);
                                },
                            },
                            { dataField: 'SourcePath', caption: 'Source Path', width: '500px', dataType: 'string', visible: false }
                        ],
                        columnChooser: { enabled: true },
                        searchPanel: { visible: true },
                        selection: { mode: 'single' },
                        onSelectionChanged: function (e) {
                            if (e.selectedRowsData && e.selectedRowsData.length > 0) {
                                _this.selectedMirroredFileID = e.selectedRowsData[0].MirroredFileID;
                                _this.selectedSectionNo = 0;
                                _this.refreshPreview();
                            }
                            else {
                                _this.clearPreview();
                            }
                        }
                    });
                    _this.selectedMirroredFileID = undefined;
                    _this.selectedSectionNo = undefined;
                    _this.previewGrid = new RIFF.Web.Core.DataGrid(_this, {
                        columnMinWidth: 80,
                        columnAutoWidth: true,
                        scrolling: {
                            useNative: true
                        }
                    });
                    _this.sectionSelect = new RIFF.Web.Core.SelectBox(_this, {
                        valueExpr: 'id',
                        displayExpr: 'name',
                        onValueChanged: function (e) {
                            if (e.value != _this.selectedSectionNo) {
                                _this.selectedSectionNo = e.value;
                                _this.refreshPreview();
                            }
                        }
                    });
                    _this.sourcesGrid.extend({ dataSource: options.dataSources });
                    return _this;
                }
                MirrorPage.prototype.refreshPreview = function () {
                    var _this = this;
                    if (this.selectedMirroredFileID !== undefined && this.selectedSectionNo !== undefined) {
                        RIFFWebCore.RIFFPage.populateGrid(this.options.urlGetPreview, { mirroredFileID: this.selectedMirroredFileID, sectionNo: this.selectedSectionNo }, function (data) {
                            _this.previewGrid.control.option('dataSource', data.preview);
                            _this.sectionSelect.control.option('dataSource', data.sections);
                            _this.sectionSelect.control.option('value', data.selectedSection);
                        });
                    }
                };
                MirrorPage.prototype.clearPreview = function () {
                    this.selectedMirroredFileID = undefined;
                    this.selectedSectionNo = undefined;
                    this.previewGrid.control.option('dataSource', []);
                    this.sectionSelect.control.option('dataSource', []);
                };
                MirrorPage.prototype.onInitialize = function () {
                    ko.applyBindings(this);
                };
                MirrorPage.prototype.onResized = function () {
                };
                MirrorPage.prototype.onInitialized = function () {
                };
                return MirrorPage;
            }(RIFF.Web.Core.PageBase));
            System.MirrorPage = MirrorPage;
        })(System = Web.System || (Web.System = {}));
    })(Web = RIFF.Web || (RIFF.Web = {}));
})(RIFF || (RIFF = {}));
/// <reference path="../../typings/codemirror/index.d.ts" />
/// <reference path="../Core/PageBase.ts" />
var RIFF;
(function (RIFF) {
    var Web;
    (function (Web) {
        var System;
        (function (System) {
            var DataEditorPage = /** @class */ (function (_super) {
                __extends(DataEditorPage, _super);
                function DataEditorPage(options) {
                    var _this = _super.call(this) || this;
                    _this.options = options;
                    return _this;
                }
                DataEditorPage.prototype.onInitialize = function () {
                    var _this = this;
                    this.contentEditor = CodeMirror.fromTextArea($('#contenteditorbox')[0], {
                        lineNumbers: false,
                        lineWrapping: true,
                        mode: "xml",
                        extraKeys: {
                            "F11": function (cm) {
                                cm.setOption("fullScreen", !cm.getOption("fullScreen"));
                            },
                            "Esc": function (cm) {
                                if (cm.getOption("fullScreen"))
                                    cm.setOption("fullScreen", false);
                            }
                        },
                        matchTags: { bothTags: true },
                        foldGutter: true,
                        gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter"]
                    });
                    this.keyEditor = CodeMirror.fromTextArea($('#keyeditorbox')[0], {
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
                        onValueChanged: function (e) { return _this.refreshGrid(codeBehind.getValueDate()); }
                    });
                    $('#deletebutton').dxButton({ text: "Delete", onClick: function () { return _this.deleteKey(); }, icon: 'trash' });
                    $('#viewbutton').dxButton({ text: "Table", onClick: function () { return _this.viewKey(); }, icon: 'find' });
                    $('#xmlbutton').dxButton({ text: "XML", onClick: function () { return _this.xmlKey(); }, icon: 'download' });
                    $('#excelbutton').dxButton({ text: "Excel", onClick: function () { return _this.excelKey(); }, icon: 'download' });
                    $('#editbutton').dxButton({ text: "Edit", onClick: function () { return _this.edit(); }, icon: 'edit' });
                    $('#updatebutton').dxButton({ text: "Update", onClick: function () { return _this.update(); }, icon: 'save', type: 'default' });
                    $('#savebutton').dxButton({ text: "Save", onClick: function () { return _this.save(); }, icon: 'save', type: 'default' });
                    $('#copybutton').dxButton({ text: "Copy", onClick: function () { return _this.copy(); }, icon: 'add' });
                    $('#importbutton').dxButton({ text: "Import", onClick: function () { return _this._import(); }, icon: 'upload' });
                    $('#savebutton').hide();
                    $('#updatebutton').hide();
                    this.contentTypeBox = $('#contenttypebox').dxTextBox({ disabled: true }).data("dxTextBox");
                    this.keyTypeBox = $('#keytypebox').dxTextBox({ disabled: true }).data("dxTextBox");
                    this.refreshGrid(null); // latest initially
                };
                ;
                DataEditorPage.prototype.onInitialized = function () {
                };
                DataEditorPage.prototype.groupCellTemplate = function (groupCell, info) {
                    $('<div>').html(info.text).css('font-weight', 'bold').appendTo(groupCell);
                };
                DataEditorPage.prototype.editRow = function (keyType, contentType, keyReference) {
                    var _this = this;
                    $('#savebutton').hide();
                    $('#updatebutton').hide();
                    $('#editbutton').show();
                    $('#copybutton').show();
                    RIFFWebCore.RIFFPage.showLoadPanel();
                    $.getJSON(this.options.urlGetDocumentForEdit, { type: keyType, keyReference: keyReference }, function (result) {
                        _this.contentEditor.setValue(result.Content);
                        _this.keyEditor.setValue(result.Key);
                        _this.editingKeyType = keyType;
                        _this.editingContentType = contentType;
                        _this.editingKeyReference = keyReference;
                        _this.keyTypeBox.option("value", keyType);
                        _this.contentTypeBox.option("value", contentType);
                        _this.keyEditor.setOption("readOnly", true);
                        _this.contentEditor.setOption("readOnly", true);
                        RIFFWebCore.RIFFPage.hideLoadPanel();
                    });
                };
                DataEditorPage.prototype.deleteKey = function () {
                    window.location.assign(this.options.urlInvalidateEntry + '?type=' + this.editingKeyType + '&keyReference=' + this.editingKeyReference);
                };
                DataEditorPage.prototype.viewKey = function () {
                    window.location.assign(this.options.urlDataSet + '?type=' + this.editingKeyType + '&keyReference=' + this.editingKeyReference);
                };
                DataEditorPage.prototype.xmlKey = function () {
                    window.location.assign(this.options.urlExportEntry + '?type=' + this.editingKeyType + '&keyReference=' + this.editingKeyReference);
                };
                DataEditorPage.prototype.excelKey = function () {
                    window.location.assign(this.options.urlDownloadEntry + '?type=' + this.editingKeyType + '&keyReference=' + this.editingKeyReference);
                };
                DataEditorPage.prototype.update = function () {
                    RIFFWebCore.Helpers.postUserAction(this.options.urlUpdateDocument, { type: this.editingKeyType, keyReference: this.editingKeyReference, data: this.contentEditor.getValue('') }, "Catalog updated.", this.resetGrid);
                };
                DataEditorPage.prototype.resetGrid = function () {
                    this.refreshGrid(null /*this.getValueDate()*/);
                    this.editRow(this.editingKeyType, this.editingContentType, this.editingKeyReference);
                };
                DataEditorPage.prototype.copy = function () {
                    $('#updatebutton').hide();
                    $('#editbutton').hide();
                    $('#copybutton').hide();
                    $('#savebutton').show();
                    this.keyEditor.setOption("readOnly", false);
                    this.contentEditor.setOption("readOnly", false);
                };
                DataEditorPage.prototype.edit = function () {
                    $('#editbutton').hide();
                    $('#updatebutton').show();
                    this.contentEditor.setOption("readOnly", false);
                };
                DataEditorPage.prototype._import = function () {
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
                };
                DataEditorPage.prototype.save = function () {
                    var _this = this;
                    RIFFWebCore.Helpers.postUserAction(this.options.urlSaveDocument, {
                        keyType: this.editingKeyType, contentType: this.editingContentType, keyData: this.keyEditor.getValue(''),
                        contentData: this.contentEditor.getValue('')
                    }, "Catalog updated.", function () { return _this.resetGrid; });
                };
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
                DataEditorPage.prototype.trimType = function (s) {
                    if (s.lastIndexOf('.') == -1) {
                        return s;
                    }
                    return s.substring(s.lastIndexOf('.') + 1);
                };
                DataEditorPage.prototype.keyTypeValue = function (rowData) {
                    return this.trimType(rowData.KeyTypeFull);
                };
                DataEditorPage.prototype.contentTypeValue = function (rowData) {
                    return this.trimType(rowData.ContentTypeFull);
                };
                DataEditorPage.prototype.onRowClick = function (e) {
                    if (e.rowType == 'data' && e.isSelected) {
                        this.editRow(e.data.KeyTypeFull, e.data.ContentTypeFull, e.data.KeyReference);
                    }
                };
                ;
                DataEditorPage.prototype.initializeGrid = function (data, options) {
                    var _this = this;
                    $("#gridContainer").dxDataGrid({
                        hoverStateEnabled: true,
                        dataSource: data,
                        columns: [
                            { dataField: 'Plane', caption: 'Plane', width: "75px", allowEditing: false, dataType: 'string', filterValues: ['User', 'System'], visible: false },
                            { calculateCellValue: function (e) { return _this.keyTypeValue(e); }, caption: 'Key Type', width: "180px", allowEditing: false, dataType: 'string', groupIndex: 0, autoExpandGroup: true, sortIndex: 0, sortOrder: 'asc' },
                            { calculateCellValue: function (e) { return _this.contentTypeValue(e); }, caption: 'Content Type', width: "200px", allowEditing: false, dataType: 'string', sortIndex: 1, sortOrder: 'asc' },
                            { dataField: 'FriendlyString', caption: 'Path', width: "260px", visible: true, allowEditing: false, dataType: 'string', sortIndex: 2, sortOrder: 'asc' },
                            { dataField: 'ValueDate', caption: 'Latest', dataType: 'date', format: "yyyy-MM-dd", width: "85px", visible: true, allowEditing: false, alignment: "center" },
                            { dataField: 'IsValid', caption: 'Valid', width: "65px", dataType: 'boolean', allowEditing: false, visible: false },
                            { dataField: 'DataSize', caption: 'Size', width: "70px", format: 'fixedPoint', allowEditing: true, dataType: 'number' },
                            { dataField: 'UpdateTime', caption: 'Update', dataType: 'date', format: "d MMM, HH:mm", width: "75px", visible: true, allowEditing: false, alignment: "right" },
                            { dataField: 'KeyReference', visible: false, allowEditing: false, dataType: 'number' },
                        ],
                        customizeColumns: function (columns) {
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
                        onCellPrepared: function (e) {
                            if (e !== undefined && e.rowType === 'data' && e.column.caption === 'Latest') {
                                if (!e.data.IsLatest) {
                                    e.cellElement.css('color', '#777');
                                }
                            }
                        },
                        masterDetail: {
                            enabled: true,
                            template: function (container, info) {
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
                                        onRowClick: function (e) {
                                            if (e.rowType == 'data' && e.isSelected) {
                                                codeBehind.editRow(e.component.element().attr('data-keytype'), e.component.element().attr('data-contenttype'), e.data.KR);
                                            }
                                        }
                                    }).attr('data-keytype', info.data.KeyTypeFull).attr('data-contenttype', info.data.ContentTypeFull).appendTo(container);
                                });
                            }
                        },
                        onRowClick: function (e) { return _this.onRowClick(e); },
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
                    $('#gridContainer').dxDataGrid('instance').repaint();
                    $('#editingPanel').css('width', ($(window).width() - 830) + 'px');
                    //this.contentEditor.setSize(null, $(window).height() - 377);
                };
                DataEditorPage.prototype.getValueDate = function () {
                    return RIFFWebCore.Helpers.getYMD($('#datebox').dxDateBox('instance').option("value"));
                };
                DataEditorPage.prototype.refreshGrid = function (valueDate) {
                    var _this = this;
                    RIFFWebCore.RIFFPage.populateGrid(this.options.urlGetLatestDocuments, { valueDate: valueDate }, function (data) { return _this.initializeGrid(data, _this.options); });
                };
                return DataEditorPage;
            }(RIFF.Web.Core.PageBase));
            System.DataEditorPage = DataEditorPage;
        })(System = Web.System || (Web.System = {}));
    })(Web = RIFF.Web || (RIFF.Web = {}));
})(RIFF || (RIFF = {}));
//# sourceMappingURL=RIFF.js.map