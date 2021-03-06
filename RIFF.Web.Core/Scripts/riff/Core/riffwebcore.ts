﻿/// <reference path="../../typings/jquery/jquery.d.ts" />
/// <reference path="../../typings/devextreme/devextreme.d.ts" />

var RFinitialize;
var RFfinalize;
var RFresize;
var RFlayout;

module RIFFWebCore {
    export module RIFFPage {
        var isLoading = false;
        var isProcessing = false;

        export function initialize() {
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
            }

            if (typeof (RFinitialize) == "function") {
                RFinitialize();
            }
            $('#riffbodywrapper').css('visibility', 'visible');
            if (typeof (RFfinalize) == "function") {
                RFfinalize();
            }
        }

        export function populateGrid(url: string, params: Object, callback: any) {
            showLoadPanel();
            $.getJSON(url, params, function (result) {
                callback(result);
                hideLoadPanel();
            });
        }

        export function showProcessingPanel() {
            isProcessing = true;
            $('#loadPanel').dxLoadPanel({
                message: 'Processing...',
                visible: true,
                showIndicator: true
            });
        }

        export function hideProcessingPanel() {
            if (isProcessing) {
                $("#loadPanel").dxLoadPanel("instance").option("visible", false);
                isProcessing = false;
            }
        }

        export function showLoadPanel() {
            isLoading = true;
            $('#loadPanel').dxLoadPanel({
                message: 'Loading...',
                visible: true,
                showIndicator: true
            });
        }

        export function hideLoadPanel() {
            if (isLoading) {
                $("#loadPanel").dxLoadPanel("instance").option("visible", false);
                isLoading = false;
            }
        }

        export function showWorkIndicator() {
            $("#workindicator").dxLoadIndicator("instance").option("visible", true);
        }

        export function hideWorkIndicator() {
            $("#workindicator").dxLoadIndicator("instance").option("visible", false);
        }

        export function refreshSystemStatus() {
            RFlayout.refreshSystemStatus();
        }
    }
}
