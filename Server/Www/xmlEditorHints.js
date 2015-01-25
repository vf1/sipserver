
CodeMirror.xmlHints['<'] = [
    'sipServer'
];

CodeMirror.xmlHints['<sipServer><'] = [
    'portForwardings',
    'turnServers',
    'voipProviders'
];

CodeMirror.xmlHints['<sipServer><portForwardings><'] =
CodeMirror.xmlHints['<sipServer><turnServers><'] =
CodeMirror.xmlHints['<sipServer><voipProviders><'] = [
    'add',
    'clear',
    'remove'
];

CodeMirror.xmlHints['<sipServer '] = [
    'udpPort',
    'tcpPort',
    'tcpPort2',
    'isAuthorizationEnabled',
    'isAuthIntEnabled',
    'isTracingEnabled',
    'wcfServiceAddress',
    'administratorPassword',
    'activeDirectoryGroup',
    'isActiveDirectoryEnabled',
    'addToWindowsFirewall',
    'wwwPath',
    'webSocketResponseFrame',
    'isOfficeSIPFiletransferEnabled'
];

CodeMirror.xmlHints['<sipServer><portForwardings><add '] = [
    'protocol',
    'localEndpoint',
    'externalEndpoint'
];

CodeMirror.xmlHints['<sipServer><turnServers><add '] = [
    'fqdn',
    'udpPort',
    'tcpPort',
    'location'
];

CodeMirror.xmlHints['<sipServer><voipProviders><add '] = [
    'serverHostname',
    'outboundProxyHostname',
    'protocol',
    'localEndpoint',
    'username',
    'displayName',
    'authenticationId',
    'password',
    'forwardIncomingCallTo',
    'restoreAfterErrorTimeout'
];
