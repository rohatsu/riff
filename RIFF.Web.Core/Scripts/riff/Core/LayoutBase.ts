/// <reference path="../Core/PageBase.ts" />

var codeBehind: RIFF.Web.Core.PageBase;
var Globalize: any;

namespace RIFF.Web.Core {
    interface LayoutOptions {
        isPresentationMode: boolean,
        urlSetPresentationMode: string,
        urlHelp: string,
        suppressMenu: boolean,
        menuItems: Array<any>,
        urlSystemStatus: string
    }

    export class LayoutBase {
        constructor(private options: LayoutOptions) {
        }

        togglePresentationMode() {
            this.options.isPresentationMode = !this.options.isPresentationMode;
            $('#presentationmode').css('cursor', 'wait');
            RIFFWebCore.RIFFPage.showWorkIndicator();
            $.post(this.options.urlSetPresentationMode, { active: this.options.isPresentationMode }, function (data) {
                window.location.reload();
            });
        }

        refreshPresentationMode() {
            if (this.options.isPresentationMode) {
                $('#presentationmode').css('color', '#000');
                $('#presentationmode').attr('title', 'PRESENTATION MODE ACTIVE');
            }
            else {
                $('#presentationmode').css('color', '#abc');
                $('#presentationmode').attr('title', 'Click to enable Presentation Mode');
            }
        }

        refreshSystemStatus() {
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
        }

        start() {
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

            $('#presentationmode').click(() => this.togglePresentationMode());

            if (typeof codeBehind === 'undefined') {
                codeBehind = new RIFF.Web.Core.LegacyPage();
            }
            codeBehind.start();
        }
    }
}
