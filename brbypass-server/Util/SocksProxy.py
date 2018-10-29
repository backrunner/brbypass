import os, socketserver, json, http.client, asyncio, socket
from Controller import Config, Log

class SocksHandler(socketserver.BaseRequestHandler):
    def handle(self):
        Log.info("New Connection from: "+self.client_address[0])
        #config
        config = Config.load()

        while True:
            headBuffer = self.request.recv(2)
            if headBuffer:
                if headBuffer == b'\x07\x02':
                    cmd = self.request.recv(1)
                    if (cmd == b'\x01'):
                        if (len(config.password) > 0):
                            _len = self.request.recv(1)
                            length = int.from_bytes(_len,byteorder='big')
                            _password = self.request.recv(length)
                            password = _password.decode('utf-8')
                            if (password == config.password):
                                #do response for auth
                                Log.warn("Auth confirm [Right password]")
                                #  Response Message
                                #   0   1   2    3
                                #  x06 x03 CMD STATUS
                                self.request.sendall(b'\x06\x03\x02\x00')
                            else:
                                Log.warn("Auth denied [Wrong password]")
                                self.request.sendall(b'\x06\x03\x02\x01')
                        else:
                            Log.warn("Auth confirm [No password]")
                            self.request.sendall(b'\x06\x03\x02\x00')
                    elif (cmd == b'\x03'):
                        #Get contentLength
                        b_length = self.request.recv(2)


    def finish(self):
        self.request.close()
        Log.info("Connection finished at: "+self.client_address[0])

class SocksProxyServer:
    def __init__(self,host,port):
        self.host = host
        self.port = port
        self.server = socketserver.ThreadingTCPServer((host,port), SocksHandler)

    def start(self):
        Log.debug("Sock proxy server started at " + self.host + ":" + str(self.port))
        try:
            self.server.serve_forever(poll_interval=0.5)
        except KeyboardInterrupt:
            Log.warn("Server stopped because user interrupt.")
            os._exit(0)
            pass

    def stop(self):
        self.server.shutdown()
        Log.debug("Socks proxy server has been stopped.")

    def getServer(self):
        return self.server