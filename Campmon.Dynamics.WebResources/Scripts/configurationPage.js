/// <reference path="Libraries/webApiRest.js" />
/// <reference path="Libraries/knockout340.js" />
(function (global, webAPI, ko) {
    'use strict';

    global.Campmon = global.Campmon || {};
    global.Campmon.ConfigurationPage = global.Campmon.ConfigurationPage || (function () {
        function CampmonViewModel() {
            var self = this;

            self.configId = '',

            self.syncComplete = ko.observable(false);
            self.isLoading = ko.observable(true);
            self.isSyncing = ko.observable(false);            

            self.maxFields = ko.observable(200);
            self.fieldsSelected = ko.observable(0);
            self.fieldsOver = ko.pureComputed(function () {
                return self.fieldsSelected() -self.maxFields()
            });
            self.tooManyFields = ko.observable(false);

            self.clients = ko.observableArray();
            self.clientLists = ko.observableArray();
            self.views = ko.observableArray();

            self.selectedView = ko.observable();
            self.selectedClient = ko.observable();
            self.selectedList = ko.observable();
            self.listType = ko.observable("existingList");
            self.newListName = ko.observable();
            self.confirmedOptIn = ko.observable('false');
            self.optInType = ko.observable();
            self.hasConnectionError = ko.observable(false);

            self.selectedPrimaryEmail = ko.observable();

            self.isDisconnecting = ko.observable(false);
            self.changeDisconnectingStatus = ko.observable();
            self.disconnect = function () {
                self.isLoading(true);
                Campmon.Plugin.executeAction('disconnect', JSON.stringify(data))
                    .then(function (result) {
                        window.location = 'landing.html';
                    }, function (error) {
                        self.isLoading(false);
                        Xrm.Utility.alertDialog("An error occured " + error);
                    });
            };

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

            self.isConfigValid = ko.computed(function () {
                return self.selectedClient()
                    && (self.selectedList() || self.newListName());
            });

            self.hasError = ko.observable(false);
            self.errorMessage = ko.observable("");
            self.criticalError = ko.observable(false);
            self.hasConnectionError = ko.observable(false);

            self.ResetConfig = function () {
                var fields = self.fields();

                fields.forEach(function (field) {
                    field.IsChecked(field.IsRecommended());
                });

                self.fields(fields);

                self.syncDuplicateEmails("true");
                self.selectedPrimaryEmail("778230000");
                self.selectedView(null);
                self.email1Selected = ko.observable(true);
                self.email2Selected = ko.observable(true);
                self.email3Selected = ko.observable(true);
            };

            self.saveAndSync = function () {
                debugger;

                if (self.fieldsSelected() > self.maxFields()) {
                    self.tooManyFields(true);
                    return;
                }

                self.isSyncing(true);
                var data = {
                    Id: self.configId,
                    Error: null,
                    BulkSyncInProgress: true,
                    ConfigurationExists: false,
                    Clients: [{
                        ClientId: self.selectedClient().ClientID,
                        Name: self.selectedClient().Name,
                    }],
                    Fields: self.fields()
                        .filter(function (f) { return f.IsChecked() })
                        .map(function (f) { return {
                            LogicalName: f.LogicalName(),
                            DisplayName: f.DisplayName(),
                            IsChecked: f.IsChecked(),
                            IsRecommended: false
                        }
                    }),
                    Views: self.selectedView() ? [{
                        ViewId: self.selectedView() ? self.selectedView().ViewId : null,
                        ViewName: self.selectedView() ? self.selectedView().ViewName : null,
                        IsSelected: true
                    }] : null,
                    SyncDuplicateEmails: self.syncDuplicateEmails(),
                    ConfirmedOptIn: self.confirmedOptIn() == 'true',
                    SubscriberEmail: self.selectedPrimaryEmail(),
                    ClientId: null,
                    ClientName: null,
                    ListId: null,
                    ListName: null
                };

                data.Lists = self.selectedList()
                    ? [{ ListId: self.selectedList().ListID, Name: self.selectedList().Name }]
                    : [{ ListId: null, Name: self.newListName()}];

                Campmon.Plugin.executeAction('saveconfiguration', JSON.stringify(data))
                    .then(function (result) {
                        //self.isSyncing(false);
                        //self.syncComplete(true);
                        debugger;
                    }, function (error) {
                        debugger;
                        console.log(error.response.text);
                        self.isSyncing(false);
                        self.errorMessage("Error saving configuration.");
                        self.hasError(true);
                    });
            };
        }

        function init() {
            var vm = new CampmonViewModel();

            vm.changeDisconnectingStatus.subscribe(function () {
                vm.isDisconnecting(!vm.isDisconnecting());
            });

            //vm.disconnect.subscribe(function () {
            //    Campmon.Plugin.executeAction('disconnect', '')
            //        .then(function (result) {
            //            // todo: go back to OAuth page
            //        }, function (error) {
            //            vm.errorMessage("Error disconnecting from Campaign Monitor.");
            //            vm.hasError(true);
            //        });
            //});

            vm.fieldChanged.subscribe(function (field) {
                debugger;
                if (field.IsChecked()) {
                    vm.fieldsSelected(vm.fieldsSelected() + 1);

                    if (vm.fieldsSelected() > vm.maxFields()) {
                        vm.tooManyFields(true);
                    }
                } else {
                    vm.fieldsSelected(vm.fieldsSelected() - 1);
                }

                return verifyFieldChange(vm, field);                               
            });

            ko.applyBindings(vm);

            Campmon.Plugin.executeAction('loadmetadata', "")
                .then(function (result) {
                    var config = JSON.parse(result.body.OutputData);

                    if (config.Error) {
                        vm.errorMessage(config.Error);
                        vm.hasError(true);
                        vm.criticalError(true);
                        return;
                    }
                    debugger;
                    vm.configId = config.Id

                    if (config.Clients) {
                        vm.clients(config.Clients);
                    }

                    if (vm.clients().length == 1) {
                        vm.selectedClient(vm.clients()[0]);
                    } else {
                        selectClient(vm, config.ClientId, config.ClientName);
                    }
                    
                    if (config.Lists) {
                        vm.clientLists(config.Lists);
                    }

                    if (config.ListId && config.ListName) {                        
                        selectList(vm, config.ListId, config.ListName);                        
                    }

                    if (config.SubscriberEmail) {
                        vm.selectedPrimaryEmail(config.SubscriberEmail.toString());
                    }

                    addAndSelectView(vm, config.Views);
                    addFields(vm, config.Fields);

                    vm.syncDuplicateEmails(config.SyncDuplicateEmails.toString());

                    vm.selectedClient.subscribe(function (selectedClient) {
                        vm.isLoading(true);
                        Campmon.Plugin.executeAction('getclientlist', selectedClient.ClientID)
                            .then(function (result) {

                                var lists = JSON.parse(result.body.OutputData);
                                if (lists.length <= 0) {
                                    vm.listType("newList");
                                } else {
                                    vm.clientLists(JSON.parse(result.body.OutputData));
                                    vm.listType("existingList");
                                }
                                vm.ResetConfig();
                                vm.isLoading(false);
                            }, function (error) {
                                vm.errorMessage("Error retrieving lists for selected client.")
                                vm.hasError(true);
                            });
                    });

                    vm.selectedList.subscribe(function (selectedList) {
                        vm.ResetConfig();
                    });

                    vm.isLoading(false);

                    if (config.BulkSyncInProgress) {
                        vm.isSyncing(true);
                    }
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
                    selectedViewId = view.ViewId;
                }
            });

            vm.views(viewsArr);
            vm.selectedView(selectedViewId);
        }

        function addFields(vm, fields) {
            var fieldArr = [];
            var countSelected = 0;
            fields.forEach(function (field) {
                fieldArr.push(new vm.ObservableField(field.LogicalName, field.DisplayName, field.IsChecked, field.IsRecommended));

                if (field.IsChecked) {
                    countSelected++;
                }

                if (field.LogicalName === "emailaddress1") {
                    vm.email1Selected(field.IsChecked);
                } else if (field.LogicalName === "emailaddress2") {
                    vm.email2Selected(field.IsChecked);
                } else if (field.LogicalName === "emailaddress3") {
                    vm.email3Selected(field.IsChecked);
                }
            });

            vm.fieldsSelected(countSelected);
            vm.fields(fieldArr);
        }

        function verifyFieldChange(vm, field) {
            var EMAIL1VAL = "778230000";
            var EMAIL2VAL = "778230001";
            var EMAIL3VAL = "778230002";
            var ERROR_NONESELECTED = "At least one of the email fields must be selected in order to sync with Campaign Monitor.";

            if (field.LogicalName() === "emailaddress1") {
                if (!vm.email2Selected() && !vm.email3Selected() && !field.IsChecked()) {
                    vm.hasError(true);
                    vm.errorMessage(ERROR_NONESELECTED);
                    field.IsChecked(!field.IsChecked());
                    return false;
                }

                if (!field.IsChecked() && vm.selectedPrimaryEmail() === EMAIL1VAL) {
                    vm.selectedPrimaryEmail(vm.email2Selected()
                                            ? EMAIL2VAL
                                            : EMAIL3VAL)
                }
                vm.email1Selected(field.IsChecked());
            } else if (field.LogicalName() === "emailaddress2") {
                if (!vm.email1Selected() && !vm.email3Selected() && !field.IsChecked()) {
                    vm.hasError(true);
                    vm.errorMessage(ERROR_NONESELECTED);
                    field.IsChecked(!field.IsChecked());
                    return false;
                }

                if (!field.IsChecked() && vm.selectedPrimaryEmail() === EMAIL2VAL) {
                    vm.selectedPrimaryEmail(vm.email1Selected()
                                            ? EMAIL1VAL
                                            : EMAIL3VAL)
                }

                vm.email2Selected(field.IsChecked());
            } else if (field.LogicalName() === "emailaddress3") {
                if (!vm.email1Selected() && !vm.email2Selected() && !field.IsChecked()) {
                    vm.hasError(true);
                    vm.errorMessage(ERROR_NONESELECTED);
                    field.IsChecked(!field.IsChecked());
                    return false;
                }

                if (!field.IsChecked() && vm.selectedPrimaryEmail() === EMAIL3VAL) {
                    vm.selectedPrimaryEmail(vm.email1Selected()
                                            ? EMAIL1VAL
                                            : EMAIL2VAL)
                }

                vm.email3Selected(field.IsChecked());
            }
            return true;
        }

        function selectClient(vm, clientId, clientName) {
            var client = ko.utils.arrayFilter(vm.clients(), function (cl) {
                return cl.ClientID === clientId && cl.Name === clientName;
            });

            if (client.length > 0) {
                vm.selectedClient(client[0]);
            }
        }

        function selectList(vm, listId, listName) {
            var list = ko.utils.arrayFilter(vm.clientLists(), function (l) {
                return l.ListID === listId && l.Name === listName;
            });

            if (list.length > 0) {
                vm.selectedList(list[0]);
            }
        }

        return {
            init: init
        };
    })();

    global.Campmon.Plugin = global.Campmon.Plugin || (function () {
        var _actionName = 'campmon_ExecuteOperationAction';

        var pluginInput = function (operation, data) {
            return {
                'OperationName': operation,
                'InputData': data
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
