/// <reference path="../../typings/knockout/knockout.d.ts" />
/// <reference path="../../typings/knockout.mapping/knockout.mapping.d.ts" />
/// <reference path="Controls.ts" />
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
//# sourceMappingURL=PageBase.js.map