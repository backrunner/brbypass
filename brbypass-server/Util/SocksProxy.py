import socket, asyncio, binascii,http.client,json,socketserver,os
from Controller import Config, Log

class SocksHandler(socketserver.BaseRequestHandler):
    def handle(self):
        Log.info("New Connection from: "+self.client_address[0])
        while True:
            try:
                headBuffer = self.request.recv(2)
            except Exception as e:
                Log.error("A connection error occured: "+str(e))
            if headBuffer:
                if headBuffer == b'\x07\x02':
                    cmd = self.request.recv(1)
                    if (cmd == b'\x01'):
                        if (len(Config.config.password) > 0):
                            _len = self.request.recv(1)
                            length = int.from_bytes(_len, byteorder='little')
                            _password = self.request.recv(length)
                            password = _password.decode('utf-8')
                            if (password == Config.config.password):
                                #  Response Message
                                #   0   1   2    3
                                #  x06 x03 CMD STATUS
                                self.request.sendall(b'\x06\x03\x02\x00')
                            else:
                                self.request.sendall(b'\x06\x03\x02\x01')
                        else:
                            self.request.sendall(b'\x06\x03\x02\x00')
                    elif (cmd == b'\x03'):
                        # Get contentLength
                        b_length = self.request.recv(2)
                        contentLength = int.from_bytes(
                            b_length, byteorder='little')
                        b_type = self.request.recv(1)
                        if (b_type == b'\x01'):
                            # process header
                            address = ""
                            for i in range(4):
                                address = address + str(int.from_bytes(self.request.recv(1), byteorder='little'))
                            b_port = self.request.recv(2)
                            port = int.from_bytes(b_port, byteorder='little')

                            # process content
                            b_content = self.request.recv(contentLength)
                            self.doproxy(request=self.request,address=address,domain=None,port=port,data=b_content,mode='ipv4')
                        elif(b_type == b'\x03'):
                            Log.info("Get contentLength: " + str(contentLength))
                            # process header
                            b_dLength = self.request.recv(2)
                            domainLength = int.from_bytes(
                                b_dLength, byteorder='little')
                            b_port = self.request.recv(2)
                            port = int.from_bytes(b_port, byteorder='little')
                            b_domain = self.request.recv(domainLength)
                            domain = b_domain.decode('utf-8')

                            # process content
                            b_content = self.request.recv(contentLength)
                            self.doproxy(request=self.request,address=None,domain=domain,port=port,data=b_content,mode="domain")
                        elif(b_type == b'\x05'):
                            # process header
                            address = ""
                            for i in range(4):
                                address = address + binascii.b2a_hex(self.request.recv(4))
                                if i != 3:
                                    address = address+":"
                            b_port = self.request.recv(2)
                            port = int.from_bytes(b_port, byteorder='little')

                            # process content
                            b_content = self.request.recv(contentLength)
                            self.doproxy(request=self.request,address=address,domain=None,port=port,data=b_content,mode="ipv6")

    def doproxy(self, request, address, domain, port, data, mode):
        header = b'\x06\x03\x04'
        if (mode == 'ipv6' or (address is not None and len(address)>15)):
            sock = socket.socket(socket.AF_INET6, socket.SOCK_STREAM)
        else:
            sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        sock.settimeout(30)
        Log.info("Domain: "+domain)
        if (address is None):
            try:
                host = socket.gethostbyname(domain)
            except:
                Log.error("Cannot resolve host.")
                sock.close()
                return
            Log.info("HOST: "+str(host))
            try:
                sock.connect((host,port))
            except Exception as e:
                Log.error(
                'A error occured when connected to remote server: ' + str(e))
                sock.close()
                return
        else:
            try:
                sock.connect((address, port))
            except Exception as e:
                Log.error(
                'A error occured when connected to remote server: ' + str(e))
                sock.close()
                return
        sock.sendall(data)
        Log.info("Data has sent to remote: " + str(data))
        while True:
            try:
                recv = sock.recv(4096)
            except Exception as e:
                Log.error(
                    "A error occured when received data from remote: " + str(e))
                sock.close()
                return
            recv_len = len(recv)
            if (recv_len > 0):
                Log.info("Data received: " + str(recv))
                b_sendLen = recv_len.to_bytes(2, byteorder='little')
                request.sendall(header+b_sendLen+recv)
                Log.info("Data has sent to local.")

    def finish(self):
        Log.info("Connection finished at: "+self.client_address[0])


class SocksProxyServer:
    def __init__(self, host, port):
        self.host = host
        self.port = port
        self.server = socketserver.ThreadingTCPServer(
            (host, port), SocksHandler)

    def start(self):
        Log.debug("Sock proxy server started at " +
                  self.host + ":" + str(self.port))
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
