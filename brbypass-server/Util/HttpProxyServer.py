import os
import socketserver
import json
import http.client
import asyncio
from Controller import Config, Log
from Util import HttpDumper

class HttpProxyHandler(socketserver.StreamRequestHandler):
    def handle(self):
        Log.info("New Client: "+self.client_address[0])
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)
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
                    return
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
                        return
            elif(data_resolved['type']=='http'):
                packet = data_resolved['content']
                fisrtLine = HttpDumper.GetFirstLine(packet)
                #get event loop
                tasks = []
                loop = asyncio.get_event_loop()
                if (fisrtLine[0] == 'CONNECT'):
                    tasks.append(self.do_CONNECT())
                elif (fisrtLine[0] == 'GET'):
                    tasks.append(self.do_GET(fisrtLine[1],HttpDumper.GetHost(packet)))
                loop.run_until_complete(asyncio.wait(tasks))

    async def do_CONNECT(self):
        jsonObj = {
            'type':'httpconnect',
            'status':'ok',
            'content': "HTTP/1.1 200 Connection established"
        }
        self.wfile.write(bytes(json.dumps(jsonObj), encoding = "utf8"))

    async def do_GET(self,address,host):
        conn = http.client.HTTPConnection(host)
        conn.request('GET',address)
        response = conn.getresponse()
        #write response to local server
        while not response.closed:
            r = response.read(1024)
            if len(r)>0:
                jsonObj = {
                    'type':'http',
                    'status':'notend',
                    'content': r.hex()
                }
                self.wfile.write(bytes(json.dumps(jsonObj), encoding = "utf8"))
        jsonObj = {
            'type':'http',
            'status':'end'
        }
        self.wfile.write(bytes(json.dumps(jsonObj), encoding = "utf8"))

    def finish(self):
        self.request.close()
        Log.info("Client finished: "+self.client_address[0])

class HttpProxyServer:
    def __init__(self,host,port):
        self.host = host
        self.port = port
        self.server = socketserver.ThreadingTCPServer((host,port), HttpProxyHandler)

    def start(self):
        Log.debug("HttpProxyServer started at " + self.host + ":" + str(self.port))
        try:
            self.server.serve_forever(poll_interval=0.5)
        except KeyboardInterrupt:
            Log.warn("Server stopped because user interrupt")
            os._exit(0)
            pass

    def stop(self):
        self.server.shutdown()
        Log.debug("HttpProxyServer stopped")

    def getServer(self):
        return self.server