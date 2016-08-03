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

            self.selectedClient = ko.observable();
            self.selectedList = ko.observable();
            self.hasConnectionError = ko.observable(false);

            self.selectedPrimaryEmail = ko.observable();

            self.isDisconnecting = ko.observable(false);
            self.changeDisconnectingStatus = ko.observable();
            self.disconnect = ko.observable();

            self.fields = ko.observableArray();

            self.syncDuplicateEmails = ko.observable();
          
            // clean these up once fields are correctly implemented
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
            
            ko.applyBindings(vm);

            Campmon.Plugin.executeAction('loadmetadata', null)
                .then(function (result) {
                    var config = JSON.parse(result.body.OutputData);
                    if (config.Error) {
                        alert(config.Error);
                    }
                    vm.clients(config.Clients);

                    if (vm.clients().length == 1) {
                        vm.selectedClient(vm.clients()[0]);
                    }

                    if (config.SubscriberEmail) {
                        vm.selectedPrimaryEmail(config.SubscriberEmail.toString());
                    }
                    
                    // ko best practices: use the underlying array as to not repaint every time a field is added
                    var fieldArr = vm.fields();
                    fieldArr.push(new ObservableField("email", "Email", true, true));
                    fieldArr.push(new ObservableField("emailaddress2", "Email Address 2", true, true));
                    fieldArr.push(new ObservableField("emailaddress3", "Email Address 3", true, true));

                    function ObservableField(logicalName, displayName, isChecked, isRecommended) {
                        var self = this;
                        self.LogicalName = ko.observable(logicalName);
                        self.DisplayName = ko.observable(displayName);
                        self.IsChecked = ko.observable(isChecked);
                        self.IsRecommended = ko.observable(isRecommended);
                    }

                    if (config.Fields) {                        
                        for (var field in config.Fields) {
                            fieldArr.push(new ObservableField(field.LogicalName, field.DisplayName, field.IsChecked, field.IsRecommended));
                        }                        
                    }

                    vm.fields.valueHasMutated();

                    vm.fields().every(function (f) {
                        debugger;
                        if (f.LogicalName() === "email") {
                            f.IsChecked.subscribe(function (newVal) { vm.email1Selected(newVal); });
                        } else if (f.LogicalName() === "emailaddress2") {
                            f.IsChecked.subscribe(function (newVal) { debugger; vm.email2Selected(newVal); });
                        } else if (f.LogicalName() === "emailaddress3") {
                            f.IsChecked.subscribe(function (newVal) { vm.email3Selected(newVal); });
                        }
                    });

                    if (config.SyncDuplicateEmails) {
                        vm.syncDuplicateEmails(config.SyncDuplicateEmails.toString());
                    }
                    
                    vm.isLoading(false);
                }, function (error) {
                    vm.hasConnectionError = true;
                    console.log(JSON.parse(error.response.text));
                });
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
