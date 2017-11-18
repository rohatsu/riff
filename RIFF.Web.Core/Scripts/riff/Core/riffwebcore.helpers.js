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
//# sourceMappingURL=riffwebcore.helpers.js.map