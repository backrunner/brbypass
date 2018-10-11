import socketserver
import json
from Controller import Config, Log

class HttpProxyHandler(socketserver.StreamRequestHandler):
    def handle(self):
        Log.info("New Client: "+self.client_address[0])
        config = Config.load()
        while True:
            data = None
            data_resolved = None
            try:
                data = self.rfile.readline().strip()
                if data:
                    try:
                        data_resolved = json.loads(data, encoding="utf8")
                    except ValueError:
                        Log.error('Cannot resolve request')
                        continue
                else:
                    continue
            except:
                Log.error('Read request error: '+str(data))
                self.finish()
                return
            if (data_resolved['type'] == 'handshake'):
                Log.debug("Got handshake: " + self.client_address[0])
                response = {
                    'type':'handshake',
                    'status':'success'
                }
                try:
                    self.wfile.write(bytes(json.dumps(response), encoding = "utf8"))
                except:
                    Log.error('Write handshake to stream error')
                    self.finish()
            elif (data_resolved['type'] == 'auth'):
                Log.debug("Got auth: " + self.client_address[0])
                if (config.password == ""):
                    response = {
                        'type':'auth',
                        'status':'success'
                    }
                    self.wfile.write(bytes(json.dumps(response), encoding = "utf8"))
                    Log.debug("Auth success: no password")
                else:
                    password = data_resolved['content']
                    if (password == config.password):
                        response = {
                            'type':'auth',
                            'status':'success'
                        }
                        self.wfile.write(bytes(json.dumps(response), encoding = "utf8"))
                        Log.debug("Auth success: password correct")
                    else:
                        response = {
                            'type':'auth',
                            'status':'failed'
                        }
                        self.wfile.write(bytes(json.dumps(response), encoding = "utf8"))
                        Log.debug("Auth failed: password wrong")
                        self.finish()

    def finish(self):
        self.request.close()
        Log.info("Client Disconnect: "+self.client_address[0])

class HttpProxyServer:
    def __init__(self,host,port):
        self.host = host
        self.port = port
        self.server = socketserver.ThreadingTCPServer((host,port), HttpProxyHandler)

    def start(self):
        Log.debug("HttpProxyServer started at " + self.host + ":" + str(self.port))
        self.server.serve_forever()

    def stop(self):
        self.server.shutdown()
        Log.debug("HttpProxyServer stopped")

    def getServer(self):
        return self.server