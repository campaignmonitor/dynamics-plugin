/// <reference path="Libraries/webApiRest.js" />
/// <reference path="Libraries/knockout340.js" />
(function (global, webAPI, ko) {
    'use strict';

    global.Campmon = global.Campmon || {};
    global.Campmon.ConfigurationPage = global.Campmon.ConfigurationPage || (function () {
        function CampmonViewModel() {
            var self = this;
            self.isLoading = ko.observable(true);

            self.clients = ko.observableArray();
            self.clientLists = ko.observableArray();
            self.views = ko.observableArray();

            self.selectedView = ko.observable();
            self.selectedClient = ko.observable();
            self.selectedList = ko.observable();
            self.hasConnectionError = ko.observable(false);

            self.selectedPrimaryEmail = ko.observable();

            self.isDisconnecting = ko.observable(false);
            self.changeDisconnectingStatus = ko.observable();
            self.disconnect = ko.observable();

            self.fields = ko.observableArray();
            self.fieldChanged = ko.observable();

            self.syncDuplicateEmails = ko.observable();

            self.ObservableField = function (logicalName, displayName, isChecked, isRecommended) {
                var self = this;
                self.LogicalName = ko.observable(logicalName);
                self.DisplayName = ko.observable(displayName);
                self.IsChecked = ko.observable(isChecked);
                self.IsRecommended = ko.observable(isRecommended);
            };

            self.email1Selected = ko.observable(true);
            self.email2Selected = ko.observable(true);
            self.email3Selected = ko.observable(true);
        }

        function init() {
            var vm = new CampmonViewModel();

            vm.selectedClient.subscribe(function (selectedClient) {
                Campmon.Plugin.executeAction('getclientlist', selectedClient)
                    .then(function (result) {
                        //TODO: If no client lists default to Sync to New List Option
                        vm.clientLists(JSON.parse(result.body.OutputData));
                    }, function (error) {
                        alert("Error retrieving lists for selected client.");
                    });
            });

            vm.changeDisconnectingStatus.subscribe(function () {
                vm.isDisconnecting(!!!vm.isDisconnecting());
            });

            vm.disconnect.subscribe(function () {
                Campmon.Plugin.executeAction('disconnect', '')
                    .then(function (result) {
                        // todo: go back to OAuth page
                    }, function (error) {
                        alert("Error: " + error);
                    });
            });

            vm.fieldChanged.subscribe(function (field) {
                if (field.LogicalName() === "emailaddress1") {
                    if (!vm.email2Selected() && !vm.email3Selected() && !field.IsChecked()) {
                        alert("At least one of the email fields must be selected in order to sync with Campaign Monitor.");
                        field.IsChecked(!field.IsChecked());
                        return false;
                    }

                    if (!field.IsChecked() && vm.selectedPrimaryEmail() === "778230000") {
                        if (vm.email2Selected()) {
                            vm.selectedPrimaryEmail("778230001");
                        } else {
                            vm.selectedPrimaryEmail("778230002");
                        }
                    }
                    vm.email1Selected(field.IsChecked());
                } else if (field.LogicalName() === "emailaddress2") {
                    if (!vm.email1Selected() && !vm.email3Selected() && !field.IsChecked()) {
                        alert("At least one of the email fields must be selected in order to sync with Campaign Monitor.");
                        field.IsChecked(!field.IsChecked());
                        return false;
                    }

                    if (!field.IsChecked() && vm.selectedPrimaryEmail() === "778230001") {
                        if (vm.email1Selected()) {
                            vm.selectedPrimaryEmail("778230000");
                        } else {
                            vm.selectedPrimaryEmail("778230002");
                        }
                    }

                    vm.email2Selected(field.IsChecked());
                } else if (field.LogicalName() === "emailaddress3") {
                    if (!vm.email1Selected() && !vm.email2Selected() && !field.IsChecked()) {
                        alert("At least one of the email fields must be selected in order to sync with Campaign Monitor.");
                        field.IsChecked(!field.IsChecked());
                        return false;
                    }

                    if (!field.IsChecked() && vm.selectedPrimaryEmail() === "778230002") {
                        if (vm.email1Selected()) {
                            vm.selectedPrimaryEmail("778230000");
                        } else {
                            vm.selectedPrimaryEmail("778230001");
                        }
                    }

                    vm.email3Selected(field.IsChecked());
                }

                return true;
            });

            ko.applyBindings(vm);

            Campmon.Plugin.executeAction('loadmetadata', "")
                .then(function (result) {
                    var config = JSON.parse(result.body.OutputData);

                    if (config.Error) {
                        alert(config.Error);
                        return;
                    }

                    if (config.Clients) {
                        vm.clients(config.Clients);
                    }

                    if (vm.clients().length == 1) {
                        vm.selectedClient(vm.clients()[0]);
                    }

                    if (config.SubscriberEmail) {
                        vm.selectedPrimaryEmail(config.SubscriberEmail.toString());
                    }

                    addAndSelectView(vm, config.Views);
                    addFields(vm, config.Fields);

                    vm.syncDuplicateEmails(config.SyncDuplicateEmails.toString());
                    vm.isLoading(false);
                }, function (error) {
                    vm.hasConnectionError(true);
                    console.log(JSON.parse(error.response.text));
                });
        }

        function addAndSelectView(vm, views) {
            var viewsArr = [];
            var selectedViewId = "";

            views.forEach(function (view) {
                viewsArr.push(view);
                if (view.IsSelected) {
                    selectedView = view.ViewId;
                }
            });

            vm.views(viewsArr);
            vm.selectedView(selectedViewId);
        }

        function addFields(vm, fields) {
            var fieldArr = [];
            fields.forEach(function (field) {
                fieldArr.push(new vm.ObservableField(field.LogicalName, field.DisplayName, field.IsChecked, field.IsRecommended));

                if (field.LogicalName === "emailaddress1") {
                    vm.email1Selected(field.IsChecked);
                } else if (field.LogicalName === "emailaddress2") {
                    vm.email2Selected(field.IsChecked);
                } else if (field.LogicalName === "emailaddress3") {
                    vm.email3Selected(field.IsChecked);
                }
            });
            vm.fields(fieldArr);
        }

        return {
            init: init
        };
    })();

    global.Campmon.Plugin = global.Campmon.Plugin || (function () {
        var _actionName = 'campmon_ExecuteOperationAction';

        var pluginInput = function (operation, data) {
            return {
                OperationName: operation,
                InputData: data
            };
        }

        var executeAction = function (action, data) {
            return webAPI.REST.executeUnboundAction(_actionName, pluginInput(action, data), null);
        }

        return {
            executeAction: executeAction
        }
    })();

})(this, webAPI, ko);
