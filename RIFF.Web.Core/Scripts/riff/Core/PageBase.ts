/// <reference path="Controls.ts" />

namespace RIFF.Web.Core {
    export abstract class PageBase {
        constructor() {
        }

        private resizeHandle: number;

        private controls: IControl[] = [];

        public registerControl(c: IControl)
        {
            this.controls.push(c);
        }

        start() {
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
            }

            this.onInitialize();

            this.resizeHandler();
            $('#riffbodywrapper').css('visibility', 'visible');

            this.onInitialized();

            $(window).resize(() => {
                clearTimeout(this.resizeHandle);
                this.resizeHandle = setTimeout(this.resizeHandler, 250);
            });
        }

        // code to run during initialization (before content is shown)
        abstract onInitialize();

        // run after finalization (content shown)
        abstract onInitialized();

        resizeHandler = () =>
        {
            $('body').height(window.document.documentElement.clientHeight);
            if (this.controls.length > 0)
            {
                this.controls.forEach(c => c.repaint());
            }
            this.onResized();
        }

        // browser resized
        onResized() { }

        // error thrown
        onError(msg: string | Event): boolean {
            return false;
        }

        populateGrid(url: string, params: string, callback: any) {
            RIFFWebCore.RIFFPage.populateGrid(url, params, callback);
        }

        showProcessingPanel() {
            RIFFWebCore.RIFFPage.showProcessingPanel();
        }

        hideProcessingPanel() {
            RIFFWebCore.RIFFPage.hideProcessingPanel();
        }

        showLoadPanel() {
            RIFFWebCore.RIFFPage.showLoadPanel();
        }

        hideLoadPanel() {
            RIFFWebCore.RIFFPage.hideLoadPanel();
        }

        showWorkIndicator() {
            RIFFWebCore.RIFFPage.showWorkIndicator();
        }

        hideWorkIndicator() {
            RIFFWebCore.RIFFPage.hideWorkIndicator();
        }
    }

    export class LegacyPage extends PageBase {
        onInitialize() {
            if (typeof (RFinitialize) == "function") {
                RFinitialize();
            }
        }

        onInitialized() {
            if (typeof (RFfinalize) == "function") {
                RFfinalize();
            }
        }

        onResize() {
            if (typeof (RFresize) == "function") {
                RFresize();
            }
        }
    }
}
