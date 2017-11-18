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
//# sourceMappingURL=riffwebcore.js.map