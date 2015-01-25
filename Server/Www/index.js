
var createTemplate = function (name) {

    function template(item) {
        this.container = item.parent();
        this.template = Handlebars.compile(item.html());
    };

    template.prototype.empty = function () {
        this.container.empty();
        return this;
    };
    template.prototype.append = function (data) {
        this.container.append(this.template(data));
        return this;
    };
    template.prototype.get = function (data) {
        return this.template(data);
    };

    return new template($(name));
};


(function ($) {

    var ctrl = new function () {
        this.accounts = $('#accounts');
        this.authDialog = $('#auth-dialog');
        this.username = $('#username');
        this.password = $('#password');
        this.superRow = $('#super-row');
        this.accountRow = $('#account-row');
    }

    var tmpl = new function () {
        this.accounts = createTemplate('#accounts-list');
        this.error = createTemplate('#global-error');
    }

    ctrl.username.val('administrator');

    var api = new OfficeSIP();
    var role = '';

    var switchToControlPanel = function () {
        $('#page-welcome').addClass('hide');
        $('#page-control').removeClass('hide');
    }

    $('#page-welcome button[href="#login"]').on('click', function () {
        api.getRole(
            function (newRole) {
                role = newRole;

                switchToControlPanel();

                if (role === 'super') {
                    accounts.refresh();
                    settings.load();
                }
                if (role === 'admin') {
                    switchToAccount(ctrl.username.val());
                }
            }
        );
    });

    $('#page-welcome button[href="#create-account"]').on('click', function () {
        createAccountDialog.show(function (newDomainName, newPassword) {
            api.authorize(newDomainName, newPassword);
            switchToControlPanel();
            switchToAccount(newDomainName);
        });
    });

    api.unauthorized = function () {
        ctrl.authDialog.modal({ backdrop: 'static', show: true });
    };

    ctrl.authDialog.find('.btn-primary').on('click', function () {
        ctrl.authDialog.authorizeClicked = true;
        ctrl.authDialog.modal('hide');
        api.authorize(ctrl.username.val(), ctrl.password.val());
    });

    ctrl.authDialog.on('hide', function () {
        if (!ctrl.authDialog.authorizeClicked)
            api.noAuthorize();
        ctrl.authDialog.authorizeClicked = false;
    });

    api.error = function (url, error) {
        console.log('error: ' + url);
        console.log(error);
        if (typeof error === 'undefined')
            error = { message: 'Unspecified error for api.url: ' + url };
        tmpl.error.append(error);
    }

    var getVersion = function () {
        api.getVersion(function () {
            $('#version .major').text(data.major);
            $('#version .minor').text(data.minor);
            $('#version .majorRevision').text(data.majorRevision);
            $('#version .minorRevision').text(data.minorRevision);
        }
        );
    };


    var switchTab = function (e) {
        e.preventDefault();
        $(this).tab('show');
        if ($(this).attr('href') === '#server-settings')
            settings.updateUI();
    };

    $('#server-tab a').on('click', switchTab);
    $('#account-tab a').on('click', switchTab);

    ctrl.superRow.collapse({ toggle: false });
    ctrl.accountRow.collapse({ toggle: false });

    var switchToAccount = function (name) {
        if (typeof name !== 'undefined') {
            domainName = name;
            $('#breadcrumb-account').text(domainName);
            $('#account-row a[href="#home"]').tab('show');
            account.load();
            users.getUserz();
        }

        if (ctrl.accountRow.hasClass('in') === false) {
            ctrl.superRow.collapse('hide');
            ctrl.accountRow.collapse('show');
            $('#breadcrumb-server').parent().removeClass('active');
            $('#breadcrumb-account').parent().addClass('active');
        }
    }

    var switchToSuper = function () {
        if (ctrl.superRow.hasClass('in') === false && role === 'super') {
            ctrl.superRow.collapse('show');
            ctrl.accountRow.collapse('hide');
            $('#breadcrumb-server').parent().addClass('active');
            $('#breadcrumb-account').parent().removeClass('active');
        }
    }

    $('#breadcrumb-server').on('click', function (e) {
        e.preventDefault();
        switchToSuper();
    });

    $('#breadcrumb-account').on('click', function (e) {
        e.preventDefault();
        switchToAccount();
    });


    var preparePassword = function (password) {
        if (password.length > 0)
            return api.getPasswordHash(password);
        return '';
    }

    /*
         ######   #######  ##    ## ######## #### ########  ##     ##       ########  ##        ######   
        ##    ## ##     ## ###   ## ##        ##  ##     ## ###   ###       ##     ## ##       ##    ##  
        ##       ##     ## ####  ## ##        ##  ##     ## #### ####       ##     ## ##       ##        
        ##       ##     ## ## ## ## ######    ##  ########  ## ### ##       ##     ## ##       ##   #### 
        ##       ##     ## ##  #### ##        ##  ##   ##   ##     ##       ##     ## ##       ##    ##  
        ##    ## ##     ## ##   ### ##        ##  ##    ##  ##     ##       ##     ## ##       ##    ##  
         ######   #######  ##    ## ##       #### ##     ## ##     ##       ########  ########  ######   
    */
    var confirmDialog = new function () {

        var _dialog = $('#confirm-dialog');

        var _callback = null;
        var _context = null;

        this.show = function (kind, context, callback) {

            _context = context;
            _callback = callback;

            _dialog.find('div.modal-header h3').hide().filter('.confirm-' + kind).show();
            _dialog.find('div.modal-body').children().hide().filter('.confirm-' + kind).show();
            _dialog.find('div.modal-footer').children().hide().filter('.confirm-' + kind).show();

            _dialog.modal();
        }

        var _onYes = function () {
            _dialog.modal('hide');
            if (_callback != null)
                _callback.call(_context);
        }

        _dialog.find('.btn-primary').on('click', _onYes);
    }

    /*
           ###    ########  ########        ###     ######   ######   #######  ##     ## ##    ## ######## 
          ## ##   ##     ## ##     ##      ## ##   ##    ## ##    ## ##     ## ##     ## ###   ##    ##    
         ##   ##  ##     ## ##     ##     ##   ##  ##       ##       ##     ## ##     ## ####  ##    ##    
        ##     ## ##     ## ##     ##    ##     ## ##       ##       ##     ## ##     ## ## ## ##    ##    
        ######### ##     ## ##     ##    ######### ##       ##       ##     ## ##     ## ##  ####    ##    
        ##     ## ##     ## ##     ##    ##     ## ##    ## ##    ## ##     ## ##     ## ##   ###    ##    
        ##     ## ########  ########     ##     ##  ######   ######   #######   #######  ##    ##    ##    
    */

    var createAccountDialog = new function () {

        var dialog = $('#add-account-dialog');

        this.show = function (handler) {
            dialog.afterAdd = handler;
            dialog.modal();
        }

        var _onAdd = function () {
            dialog.modal('hide');

            var handler = dialog.afterAdd;
            delete dialog.afterAdd;

            var newPassword = dialog.find('.password').val();
            var newDomainName = dialog.find('.domainName').val();

            api.postAccount(
                {
                    password: preparePassword(newPassword),
                    domainName: newDomainName,
                    email: dialog.find('.email').val()
                },
                function (data) {
                    console.log('addAccount - Ok');
                    if (typeof handler !== 'undefined' || handler != null)
                        handler(newDomainName, newPassword);

                    dialog.find('.domainName').val('');
                    dialog.find('.password').val('');
                    dialog.find('.email').val('');

                    accounts.refresh();
                }
            );
        }

        dialog.find('.btn-primary').on('click', _onAdd);
    };

    /*
           ###     ######   ######   #######  ##     ## ##    ## ########  ######  
          ## ##   ##    ## ##    ## ##     ## ##     ## ###   ##    ##    ##    ## 
         ##   ##  ##       ##       ##     ## ##     ## ####  ##    ##    ##       
        ##     ## ##       ##       ##     ## ##     ## ## ## ##    ##     ######  
        ######### ##       ##       ##     ## ##     ## ##  ####    ##          ## 
        ##     ## ##    ## ##    ## ##     ## ##     ## ##   ###    ##    ##    ## 
        ##     ##  ######   ######   #######   #######  ##    ##    ##     ######  
    */
    var accounts = new function () {

        var ctrl = new function () {
            this.accounts = $('#accounts');
            this.refresh = $('#super-accounts-toolbar button[href="#refresh"]');
        };

        this.create = function () {
            createAccountDialog.show();
        };

        this.refresh = function () {
            ctrl.refresh.button('loading');
            api.getAllAccounts(
                function (list) {
                    ctrl.refresh.button('reset');
                    ctrl.accounts.show();
                    tmpl.accounts.empty();
                    tmpl.accounts.append(list);
                }
            );
        };

        this.remove = function () {

            if (this.hasSelected())
                confirmDialog.show('delete', this, this.removeNow);
        };

        this.removeNow = function () {

            var trs = this.getSelected();

            for (var i = 0; i < trs.length; i++) {
                api.deleteAccount(
                    $(trs[i]).data('domain-name'),
                    function (data) {
                        console.log('deleteAccount - Ok');
                        accounts.refresh();
                    }
                );
            }
        };

        this.getSelected = function () {
            return tmpl.accounts.container.find('tr:has(input:checked:enabled)');
        };

        this.hasSelected = function () {
            return this.getSelected().length > 0;
        };


        this.on = function (command) {
            $('#super-accounts-menu a[href="#' + command + '"]').on('click', function (e) {
                e.preventDefault();
                accounts[command]();
            });
            $('#super-accounts-toolbar button[href="#' + command + '"]').on('click', function (e) {
                e.preventDefault();
                accounts[command]();
            });
        };

        this.on('refresh');
        this.on('create');
        this.on('remove');

        tmpl.accounts.container.on('click', 'tr', function (e) {
            if (e.target.nodeName !== 'INPUT')
                switchToAccount($(e.currentTarget).data('domain-name'));
        });
    }

    /*
         ######  ######## ######## ######## #### ##    ##  ######    ######  
        ##    ## ##          ##       ##     ##  ###   ## ##    ##  ##    ## 
        ##       ##          ##       ##     ##  ####  ## ##        ##       
         ######  ######      ##       ##     ##  ## ## ## ##   ####  ######  
              ## ##          ##       ##     ##  ##  #### ##    ##        ## 
        ##    ## ##          ##       ##     ##  ##   ### ##    ##  ##    ## 
         ######  ########    ##       ##    #### ##    ##  ######    ######  
    */

    var settings = new function () {

        var tmpl = new function () {
            this.errors = createTemplate('#server-settings-errors');
            this.noerror = createTemplate('#server-settings-noerror');
        };

        this.load = function () {

            this.clearErrors();

            api.getOptions(function (data) {
                editor.setValue(data);
                updateHelpers();
            });
        }

        this.save = function () {

            this.clearErrors();

            api.putOptions(
                editor.getValue(),
                function (errors) {
                    settings.showErrors(errors);
                });
        }

        this.validate = function () {

            this.clearErrors();

            api.validateOptions(
                editor.getValue(),
                function (errors) {
                    settings.showErrors(errors);
                });
        }

        this.updateUI = function () {
            editor.refresh();
        }

        this.clearErrors = function () {
            tmpl.errors.empty();
        }

        this.showErrors = function (errors) {
            tmpl.errors.empty();
            if (errors != null && errors.length > 0)
                tmpl.errors.append(errors);
            else
                tmpl.noerror.append();
        };

        this.on = function (command) {
            $('#super-settings-menu a[href="#' + command + '"]').on('click', function (e) {
                e.preventDefault();
                settings[command]();
            });
            $('#super-settings-toolbar button[href="#' + command + '"]').on('click', function (e) {
                e.preventDefault();
                settings[command]();
            });
        };

        this.on('load');
        this.on('save');
        this.on('validate');

        var editor = CodeMirror(
            document.getElementById("server-settings-editor"),
            {
                value: '',
                mode: 'application/xml',
                lineNumbers: true,
                extraKeys: {
                    "'>'": function (cm) { cm.closeTag(cm, '>'); },
                    "'/'": function (cm) { cm.closeTag(cm, '/'); },
                    "' '": function (cm) { CodeMirror.xmlHint(cm, ' '); },
                    "'<'": function (cm) { CodeMirror.xmlHint(cm, '<'); },
                    "Ctrl-Space": function (cm) { CodeMirror.xmlHint(cm, ''); }
                }
            });

        var changeAttribute = function (element, attribute, newValue) {

            editor.setValue(
                xmlHelper.changeAttribute(editor.getValue(), element, attribute, newValue));
        }

        var setCheckbox = function (xml, checkbox, tagName, attrName, defaultValue) {
            var attr = xml.find(tagName).attr(attrName);
            if (typeof attr === 'undefined' && typeof defaultValue !== 'undefined')
                attr = defaultValue.toString();
            if (typeof attr !== 'undefined' && attr.toUpperCase() == 'TRUE')
                checkbox.attr('checked', 'checked');
            else
                checkbox.removeAttr('checked');
        }

        var setEditbox = function (xml, editbox, tagName, attrName) {
            var attr = xml.find(tagName).attr(attrName);
            if (typeof attr !== 'undefined')
                editbox.val(attr);
            else
                editbox.val('');
        }

        var setSelectbox = function (xml, selectbox, tagName, attrName) {
            var attr = xml.find(tagName).attr(attrName);
            if (typeof attr !== 'undefined')
                selectbox.val(attr);
            else
                selectbox.val('');
        }

        var setControl = function (xml, control, tagName, attrName) {
            if (control.length > 0) {
                var element = control[0];
                if (element.tagName == 'INPUT') {
                    if (element.type == 'text')
                        setEditbox(xml, control, tagName, attrName);
                    else if (element.type == 'checkbox')
                        setCheckbox(xml, control, tagName, attrName);
                }
                else if (element.tagName == 'SELECT') {
                    setSelectbox(xml, control, tagName, attrName);
                }
            }
        }

        var updateHelpers = function () {

            var text = editor.getValue();

            var xmlValid = true;
            try {
                var xml = $($.parseXML(text));
            }
            catch (ex) {
                xmlValid = false;
            }

            if (xmlValid) {
                tracingform.update(xml);
                adform.update(xml);
                webform.update(xml);
                miscform.update(xml);
            }
        }

        editor.on('change', updateHelpers);

        var passhash = new function () {

            var password = $('#passhashform input[type="text"]');

            var updateHash = function () {
                changeAttribute('sipServer', 'administratorPassword', api.getPasswordHash(password.val()));
            };

            password.on('change', updateHash);
            password.on('input', updateHash);
        }

        var tracingform = new function () {

            var isEnabled = $('#tracingform input[type="checkbox"]');
            var path = $('#tracingform input[type="text"]');

            this.update = function (xml) {
                setCheckbox(xml, isEnabled, 'sipServer', 'isTracingEnabled');
                setEditbox(xml, path, 'sipServer', 'tracingPath');
            }

            var updateIsEnabled = function () {
                changeAttribute('sipServer', 'isTracingEnabled', isEnabled.is(':checked'));
            }

            var updatePath = function () {
                changeAttribute('sipServer', 'tracingPath', path.val());
            }

            isEnabled.on('change', updateIsEnabled);
            isEnabled.on('input', updateIsEnabled);
            path.on('change', updatePath);
            path.on('input', updatePath);
        }

        var adform = new function () {

            var isEnabled = $('#adform input[type="checkbox"]');
            var group = $('#adform input[type="text"]');

            this.update = function (xml) {
                setCheckbox(xml, isEnabled, 'sipServer', 'isActiveDirectoryEnabled');
                setEditbox(xml, group, 'sipServer', 'activeDirectoryGroup');
            }

            var updateIsEnabled = function () {
                changeAttribute('sipServer', 'isActiveDirectoryEnabled', isEnabled.is(':checked'));
            }

            var updateGroup = function () {
                changeAttribute('sipServer', 'activeDirectoryGroup', group.val());
            }

            isEnabled.on('change', updateIsEnabled);
            isEnabled.on('input', updateIsEnabled);
            group.on('change', updateGroup);
            group.on('input', updateGroup);
        }

        var webform = new function () {

            var path = $('#webform input[type="text"]');
            var opcode = $('#webform select');

            this.update = function (xml) {
                setEditbox(xml, path, 'sipServer', 'wwwPath');
                setSelectbox(xml, opcode, 'sipServer', 'webSocketResponseFrame');
            }

            var updatePath = function () {
                changeAttribute('sipServer', 'wwwPath', path.val());
            }

            var updateOpcode = function () {
                changeAttribute('sipServer', 'webSocketResponseFrame', opcode.val());
            }

            path.on('change', updatePath);
            path.on('input', updatePath);
            opcode.on('change', updateOpcode);
        }

        var miscform = new function () {

            var isFileTransferEnabled = $('#officesipfiletransfer');

            this.update = function (xml) {
                setCheckbox(xml, isFileTransferEnabled, 'sipServer', 'isOfficeSIPFiletransferEnabled', true);
            }

            var updateIsFileTransferEnabled = function () {
                changeAttribute('sipServer', 'isOfficeSIPFiletransferEnabled', isFileTransferEnabled.is(':checked'));
            }

            isFileTransferEnabled.on('change', updateIsFileTransferEnabled);
            isFileTransferEnabled.on('input', updateIsFileTransferEnabled);
        }
    };

    /*
           ###    ##       ########  ######## ########    ###    #### ##        ######  
          ## ##    ##      ##     ## ##          ##      ## ##    ##  ##       ##    ## 
         ##   ##    ##     ##     ## ##          ##     ##   ##   ##  ##       ##       
        ##     ##    ##    ##     ## ######      ##    ##     ##  ##  ##        ######  
        #########   ##     ##     ## ##          ##    #########  ##  ##             ## 
        ##     ##  ##      ##     ## ##          ##    ##     ##  ##  ##       ##    ## 
        ##     ## ##       ########  ########    ##    ##     ## #### ########  ######  
    */

    domainName = '';

    var account = new function () {

        var ctrl = new function () {
            this.domainName = $('#home input.domain-name');
            this.email = $('#home input.email');
            this.password = $('#home input.password');
        }

        var tmpl = new function () {
            this.saved = createTemplate('#account-details-saved');
            this.error = createTemplate('#account-details-error');
        }

        this.load = function () {

            api.getAccount(
                domainName,
                function (data) {
                    console.log('get account - ok');
                    ctrl.domainName.val(data.domainName);
                    ctrl.email.val(data.email);
                }
            );
        }

        this.save = function () {

            $("#home :input").attr("disabled", true);

            api.putAccount(
                domainName,
                {
                    domainName: ctrl.domainName.val(),
                    email: ctrl.email.val(),
                    password: preparePassword(ctrl.password.val())
                },
                function () {
                    domainName = ctrl.domainName.val();
                    tmpl.saved.empty();
                    tmpl.saved.append();
                    $("#home :input").attr("disabled", false);
                },
                function (url, details) {
                    tmpl.error.empty();
                    tmpl.error.append(details);
                    $("#home :input").attr("disabled", false);
                }
            );
        }

        this.on = function (command) {
            $('#account-details-toolbar button[href="#' + command + '"]').on('click', function (e) {
                e.preventDefault();
                account[command]();
            });
        };

        this.on('load');
        this.on('save');
    };

    /*
           ###    ##       ##     ##  ######  ######## ########   ######  
          ## ##    ##      ##     ## ##    ## ##       ##     ## ##    ## 
         ##   ##    ##     ##     ## ##       ##       ##     ## ##       
        ##     ##    ##    ##     ##  ######  ######   ########   ######  
        #########   ##     ##     ##       ## ##       ##   ##         ## 
        ##     ##  ##      ##     ## ##    ## ##       ##    ##  ##    ## 
        ##     ## ##        #######   ######  ######## ##     ##  ######  
    */

    var users = new function () {

        var ctrl = new function () {
        }

        var tmpl = new function () {
            this.tab = Handlebars.compile($('#users').html());
            this.tr = Handlebars.compile($('#users-tr').html());
            this.menu = Handlebars.compile($('#users-menu').html());
        }

        var addDialog = new function () {

            var dialog = $('#add-user-dialog');

            var usersId = '';

            this.show = function (usersId1) {
                usersId = usersId1;
                dialog.modal();
            }

            var _onAdd = function () {
                dialog.modal('hide');

                api.postUser(
                    domainName,
                    usersId,
                    {
                        name: dialog.find('.username').val(),
                        displayName: dialog.find('.displayName').val(),
                        email: dialog.find('.email').val(),
                        password: dialog.find('.password').val()
                    },
                    function () {
                        console.log('postUser - Ok');
                        users.load(usersId);
                    }
                );
            }

            dialog.find('.btn-primary').on('click', _onAdd);
        };

        this.getUserz = function () {

            api.getUserz(
                domainName,
                function (data) {
                    users.onGetUserz.call(users, data);
                }
            );
        };

        this.onGetUserz = function (userz) {

            $('.users-ui').remove();
            ctrl.users = {};

            $('#users-headline').show();

            var menu = $('#account-tab');
            var tabContent = $('#account-row .tab-content');

            for (var k in userz) {
                tabContent.append(tmpl.tab(userz[k]));
                menu.append(tmpl.menu(userz[k]));

                var id = userz[k].id;
                ctrl.users[id] = $('#' + id + '-users');

                this.on('load', id);
                this.on('add', id);
                this.on('remove', id);

                this.load(id);
            }

            menu.find('li.users-ui>a').on('click', switchTab);
        }

        this.load = function (usersId) {

            api.getUsers(
                domainName,
                usersId,
                function (users) {
                    var tbody = ctrl.users[usersId].find('tbody');
                    tbody.empty();
                    tbody.append(tmpl.tr(users));
                }
            );
        }

        this.add = function (usersId) {

            addDialog.show(usersId);
        }

        this.remove = function (usersId) {

            if (this.hasSelected(usersId))
                confirmDialog.show('delete-users', this, function () { this.removeNow(usersId); });
        }

        this.removeNow = function (usersId) {

            var trs = this.getSelected(usersId);

            for (var i = 0; i < trs.length; i++) {
                api.deleteUser(
                    domainName,
                    usersId,
                    $(trs[i]).data('username'),
                    function (data) {
                        console.log('deleteUsers - Ok');
                        users.load(usersId);
                    }
                );
            }
        };

        this.getSelected = function (usersId) {
            return ctrl.users[usersId].find('tr:has(input:checked:enabled)');
        };

        this.hasSelected = function (usersId) {
            return this.getSelected(usersId).length > 0;
        };

        this.on = function (command, usersId) {
            $('#account-' + usersId + '-users-toolbar button[href="#' + command + '"]').on('click', function (e) {
                e.preventDefault();
                users[command](usersId);
            });
        };
    }

})(jQuery);
