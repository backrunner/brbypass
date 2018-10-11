import os
import sys
from Controller import Log, Config
from Util.HttpProxyServer import HttpProxyServer

#version
version = "0.1"

#main execute
if __name__ == "__main__":
    Log.debug("Starting brbypass("+version+")...")
    config = Config.load()
    Log.debug("Config: "+config.host+":"+str(config.port)+" @ \""+config.password+"\" % "+config.mode)

    #mode change
    if config.mode == "http":
        proxyServer = HttpProxyServer(config.host, config.port)
        proxyServer.start()