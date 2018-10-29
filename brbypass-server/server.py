import os
import sys
from Controller import Log, Config
from Util import SocksProxy

#version
version = "0.1 Reborn"

#main execute
if __name__ == "__main__":
    Log.info("Starting brbypass ["+version+"]...")
    config = Config.load()
    Log.debug("Config: "+config.host+":"+str(config.port)+" @ \""+config.password+"\" % "+config.mode)

    #mode change
    if config.mode == "socks":
        proxyServer = SocksProxy.SocksProxyServer(config.host,config.port)
        proxyServer.start()