import os, sys, json
from Controller import Log

class Config:
    def __init__(self, host, port, password, mode, securekey):
        self.host = host
        self.port = port
        self.password = password
        self.mode = mode
        self.securekey = securekey

def load():
    if (os.path.exists(sys.path[0]+'/config.json')):
        f = open(sys.path[0]+'/config.json',encoding="utf-8")
        config = json.load(f)
        mode = ""
        if configModeCheck(config['mode'])>0:
            mode = config['mode']
        else:
            Log.error('Proxy mode is not set up')
            sys.exit(0)
        host = ""
        if config['host'] is None or len(config['host'])<=0:
            host = "0.0.0.0"
            Log.warn("Host is not set up, use default value: 0.0.0.0")
        else:
            host = config['host']
        port = 1234
        if config['port'] is None or config['port'] <= 0:
            Log.warn("Port is not set up, use default value: 1234")
        else:
            port = config['port']
        password = ""
        if config['password'] is None or len(config['password'])<=0:
            Log.warn("Password is not set up")
        else:
            password = config['password']
        securekey = "brbypass23333"
        if config['securekey'] is None or len(config['securekey'])<=0:
            Log.warn("Securekey is not set up, use default value: brbypass23333")
        else:
            securekey = config['securekey']
        return Config(host,port,password,mode,securekey)
    else:
        Log.error("Config file is not found")
        sys.exit(0)

def configModeCheck(mode):
    return {
        'http':1
    }.get(mode,-1)