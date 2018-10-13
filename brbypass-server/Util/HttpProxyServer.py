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
        #config
        config = Config.load()
        header = b'\x62\x72\x62\x70'
        #eventloop
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)
        #handle packet
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
                Log.error('Read request error')
                return
            Log.debug(str(data_resolved))
            if (data_resolved['type'] == 'handshake'):
                Log.debug("Got handshake: " + self.client_address[0])
                response = {
                    'type':'handshake',
                    'status':'success'
                }
                try:
                    resBuffer = bytes(json.dumps(response), encoding = "utf8")
                    bytesLength = (len(resBuffer)).to_bytes(2,byteorder='big')
                    self.wfile.write(header+bytesLength+resBuffer)
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
                    resBuffer = bytes(json.dumps(response), encoding = "utf8")
                    bytesLength = (len(resBuffer)).to_bytes(2,byteorder='big')
                    self.wfile.write(header+bytesLength+resBuffer)
                    Log.debug("Auth success: no password")
                else:
                    password = data_resolved['content']
                    if (password == config.password):
                        response = {
                            'type':'auth',
                            'status':'success'
                        }
                        resBuffer = bytes(json.dumps(response), encoding = "utf8")
                        bytesLength = (len(resBuffer)).to_bytes(2,byteorder='big')
                        self.wfile.write(header+bytesLength+resBuffer)
                        Log.debug("Auth success: password correct")
                    else:
                        response = {
                            'type':'auth',
                            'status':'failed'
                        }
                        resBuffer = bytes(json.dumps(response), encoding = "utf8")
                        bytesLength = (len(resBuffer)).to_bytes(2,byteorder='big')
                        self.wfile.write(header+bytesLength+resBuffer)
                        Log.debug("Auth failed: password wrong")
                        return
            elif(data_resolved['type']=='http'):
                packet = data_resolved['content']
                fisrtLine = HttpDumper.GetFirstLine(packet)
                #get event loop
                loop = asyncio.get_event_loop()
                if (fisrtLine[0] == 'CONNECT'):
                    Log.info("Loop start")
                    loop.run_until_complete(self.do_CONNECT())
                    Log.info("Loop end")
                elif (fisrtLine[0] == 'GET'):
                    Log.info("Loop start")
                    loop.run_until_complete(self.do_GET(fisrtLine[1],HttpDumper.GetHost(packet)))
                    Log.info("Loop end")

    async def do_CONNECT(self):
        jsonObj = {
            'type':'httpconnect',
            'status':'ok',
            'content': "HTTP/1.1 200 Connection established"
        }
        header = b'\x62\x72\x62\x70'
        resBuffer = bytes(json.dumps(jsonObj), encoding = "utf8")
        bytesLength = (len(resBuffer)).to_bytes(2,byteorder='big')
        self.wfile.write(header+bytesLength+resBuffer)

    async def do_GET(self,address,host):
        header = b'\x62\x72\x62\x70'
        conn = http.client.HTTPConnection(host)
        conn.request('GET',address)
        response = conn.getresponse()
        #write response to local server
        while not response.closed:
            r = response.read(2048)
            if len(r)>0:
                jsonObj = {
                    'type':'http',
                    'status':'notend',
                    'content': r.hex()
                }
                resBuffer = bytes(json.dumps(jsonObj), encoding = "utf8")
                bytesLength = (len(resBuffer)).to_bytes(2,byteorder='big')
                self.wfile.write(header+bytesLength+resBuffer)
            else:
                response.close()
        jsonObj = {
            'type':'http',
            'status':'end'
        }
        resBuffer = bytes(json.dumps(jsonObj), encoding = "utf8")
        bytesLength = (len(resBuffer)).to_bytes(2,byteorder='big')
        self.wfile.write(header+bytesLength+resBuffer)
        conn.close()

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