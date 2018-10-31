import os
import sys
import asyncio
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
        loop = asyncio.get_event_loop()
        if (loop is None):
            loop = asyncio.new_event_loop()
            asyncio.set_event_loop(loop)

        proxyServer = SocksProxy.SocksProxyServer(Config.config.host,Config.config.port)
        try:
            proxyServer.start(loop)
        except KeyboardInterrupt:
            proxyServer.stop(loop)
            os._exit(0)