import os
import sys
from Controller import Log, Config
from Util import SocksProxy

#version
version = "0.1 Reborn"

#main execute
if __name__ == "__main__":
    Log.info("Starting brbypass ["+version+"]...")
    Log.debug("Config: "+Config.config.host+":"+str(Config.config.port)+" @ \""+Config.config.password+"\" % "+Config.config.mode)

    #mode change
    if Config.config.mode == "socks":
        proxyServer = SocksProxy.SocksProxyServer(Config.config.host,Config.config.port)
        proxyServer.start()