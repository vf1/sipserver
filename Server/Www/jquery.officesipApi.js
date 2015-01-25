
var OfficeSIP = function() {
}

OfficeSIP.prototype.authname = null;
OfficeSIP.prototype.password = '';
OfficeSIP.prototype.unauthorized = function() { };
OfficeSIP.prototype.error = function() { };
OfficeSIP.prototype.delayedRequests = [];

OfficeSIP.prototype.authorize = function(authname, password) {

    if(typeof authname !== 'undefined')
        this.authname = authname;

    this.password = this.getPasswordHash(password);

    var delayedRequests = this.delayedRequests.slice(0);
    this.delayedRequests = [];
    for (var i = 0; i < delayedRequests.length; i++)
        this.request.apply(this, delayedRequests[i].params);
}

OfficeSIP.prototype.noAuthorize = function() {
    this.delayedRequests = [];
}

OfficeSIP.prototype.getPasswordHash = function(password) {
    return CryptoJS.enc.Hex.stringify(CryptoJS.MD5(password + '{ED3F2734-AFBA-4f5f-AD40-5F01917C5926}'));
}

OfficeSIP.prototype.getVersion = function(success, error) {
    this.request('GET', '/api/version', success, error);
}

OfficeSIP.prototype.getOptions = function(success, error) {
    this.request('GET', '/api/options', success, error);
}

OfficeSIP.prototype.putOptions = function(options, success, error) {
    this.request('PUT', '/api/options', success, error, options);
}

OfficeSIP.prototype.validateOptions = function(options, success, error) {
    this.request('POST', '/api/options', success, error, options);
}


OfficeSIP.prototype.getRole = function(success, error) {
    this.request('GET', '/api/role', success, error);
}


OfficeSIP.prototype.getAllAccounts = function(success, error) {
    this.request('GET', '/api/accounts', success, error);
}

OfficeSIP.prototype.postAccount = function(account, success, error) {
    this.request('POST', '/api/accounts', success, error, account);
}

OfficeSIP.prototype.deleteAccount = function(domain, success, error) {
    this.request('DELETE', '/api/accounts/'+domain, success, error);
}

OfficeSIP.prototype.getAccount = function(domain, success, error) {
    this.request('GET', '/api/accounts/'+domain, success, error);
}

OfficeSIP.prototype.putAccount = function(domain, account, success, error) {
    this.request('PUT', '/api/accounts/'+domain, success, error, account);
}


OfficeSIP.prototype.getUserz = function(domain, success, error) {
    this.request('POST', '/api/accounts/'+domain+'/userz', success, error);
}


OfficeSIP.prototype.getUsers = function(domain, usersId, success, error) {
    this.request('GET', '/api/accounts/'+domain+'/userz/'+usersId, success, error);
}

OfficeSIP.prototype.postUser = function(domain, usersId, user, success, error) {
    this.request('POST', '/api/accounts/'+domain+'/userz/'+usersId, success, error, user);
}

OfficeSIP.prototype.putUser = function(domain, usersId, user, success, error) {
    this.request('PUT', '/api/accounts/'+domain+'/userz/'+usersId+'/'+user.name, success, error, user);
}

OfficeSIP.prototype.deleteUser = function(domain, usersId, name, success, error) {
    this.request('DELETE', '/api/accounts/'+domain+'/userz/'+usersId+'/'+name, success, error);
}


OfficeSIP.prototype.request = function(method, url, success, error, data, context) {

    var object = this;

    $.ajax({
        type: (method == 'GET' || typeof data === 'undefined') ? 'GET' : 'POST',
        url: this._prepareUrl(url, method),
        data: (typeof data !== 'undefined' && data != null) ? JSON.stringify(data) : null,
        contentType: 'application/json',
        success: function(data, textStatus, jqXHR) {
            success.call(context, data);
        },
        error: function(jqXHR, textStatus, errorThrown) {
            if (jqXHR.status == 403) {
                object.delayedRequests.push({ params: [method, url, success, error, data, context] });
                if(object.delayedRequests.length == 1)
                    object.unauthorized();
            }
            else {
                var details;
                try {
                    if (jqXHR.responseText.length > 0)
                        details = jQuery.parseJSON(jqXHR.responseText);
                }
                catch (ex) {
                }
                if(details === 'undefined')
                    details = (typeof errorThrown !== 'undefined') ? errorThrown : textStatus;
                if (typeof error !== 'undefined' && error != null)
                    error(url, details);
                else
                    object.error.call(context, url, details);
            }
        }
    });
}

OfficeSIP.prototype._prefixParam = function(url, param) {
    return ((url.indexOf('?') == -1) ? '?' : '&') + param;
}


OfficeSIP.prototype._toHex8 = function(d) {
    var hex = ((d >= 0) ? d : (0xffffffff - d - 1)).toString(16);
    return "00000000".substr(0, 8 - hex.length) + hex; 
}

OfficeSIP.prototype._prepareUrl = function(url, method, args) {

    if(method == 'PUT' || method == 'DELETE')
        url += this._prefixParam(url, 'method=' + method);
    
    return this._sign(url);
}

OfficeSIP.prototype._sign = function(url) {

    url += this._prefixParam(url, 'time=' + new Date().getTime());
    if(this.authname != null)
        url += '&user=' + this.authname;
    url += '&sig=' + CryptoJS.HmacMD5(url, this.password);

    return url;
}
