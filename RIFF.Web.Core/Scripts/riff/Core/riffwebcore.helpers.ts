/// <reference path="../../typings/jquery/jquery.d.ts" />
/// <reference path="../../typings/devextreme/devextreme.d.ts" />

module RIFFWebCore {
    export module Helpers {
        // returns promise whether to cancel or not
        export function postUserAction(url: string, data: any, successString: string, successCallback: (any) => void = null, errorCallback: () => void = null, alwaysCallback: () => void = null) {
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
                } else if (response.RedirectUrl) {
                    window.location.assign(response.RedirectUrl);
                } else {
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

        export function postUserActionProcessing(url: string, data: any, successString: string, callback: () => any) {
            RIFFWebCore.RIFFPage.showProcessingPanel();
            $.post(url, data).done(function (response) {
                if (response.ErrorMessage) {
                    if (response.ErrorMessage.indexOf("There was no endpoint") >= 0) {
                        response.ErrorMessage = "System is offline";
                    }
                    RIFFWebCore.RIFFPage.hideProcessingPanel();
                    DevExpress.ui.notify("Error: " + response.ErrorMessage, "error", 10000);
                } else if (response.RedirectUrl) {
                    window.location.assign(response.RedirectUrl);
                } else if (successString !== undefined) {
                    RIFFWebCore.RIFFPage.hideProcessingPanel();
                    DevExpress.ui.notify(successString, "success", 2000);
                    if (callback !== undefined) {
                        callback();
                    }
                }
            });
        }

        export function postUserActionCallback(url: string, data: any, callback: (any) => any) {
            RIFFWebCore.RIFFPage.showProcessingPanel();
            $.post(url, data).done(function (response) {
                if (response.ErrorMessage) {
                    if (response.ErrorMessage.indexOf("There was no endpoint") >= 0) {
                        response.ErrorMessage = "System is offline";
                    }
                    RIFFWebCore.RIFFPage.hideProcessingPanel();
                    DevExpress.ui.notify("Error: " + response.ErrorMessage, "error", 10000);
                } else if (response.RedirectUrl) {
                    window.location.assign(response.RedirectUrl);
                } else {
                    RIFFWebCore.RIFFPage.hideProcessingPanel();
                    callback(response);
                }
            });
        }

        export function areYouSure(title: string, callback: () => any) {
            var result = DevExpress.ui.dialog.confirm("<p style='padding: 10px 20px 20px 5px;'>Are you sure you'd like to " + title + "?", 'Confirmation');
            result.done(function (dialogResult) {
                if (dialogResult) {
                    callback();
                }
            });
        }

        export function alert(message: string) {
            var result = DevExpress.ui.dialog.alert("<p style='padding: 10px 20px 20px 5px;'>" + message + "</p>", 'Alert');
        }

        export function message(message: string, callback: () => any) {
            var result = DevExpress.ui.dialog.alert("<p style='padding: 10px 20px 20px 5px;'>" + message + "</p>", 'Message');
            result.done(function (dialogResult) {
                if (dialogResult && callback) {
                    callback();
                }
            });
        }

        export function getYMD(date: any) {
            var dd = date.getDate();
            var mm = date.getMonth() + 1; //January is 0!
            var yyyy = date.getFullYear();

            if (dd < 10) {
                dd = '0' + dd
            }

            if (mm < 10) {
                mm = '0' + mm
            }

            return yyyy + '-' + mm + '-' + dd;
        }

        export function numberWithCommas(value: any, digits: number) {
            if (value || value == 0) {
                return value.toFixed(digits).toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
            }
            return '';
        }
    }
}
