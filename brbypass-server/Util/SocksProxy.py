import socket
import asyncio
import binascii
import http.client
import json
import os
import re
import ssl
import threading
from Util import namechecker
from Controller import Config, Log

clients = {}


class SocksProxyServer:

    def __init__(self,host,port):
        self.host = host
        self.port = port
        self.server = None
        self.coro = None

    def start(self, loop):        
        self.coro = asyncio.start_server(self.accept_client,self.host,self.port,loop=loop)
        self.server = loop.run_until_complete(self.coro)
        Log.info("Sock proxy server started at " +
            self.host + ":" + str(self.port))
        self.server = loop.run_forever()

    def stop(self, loop):
        if self.server is not None:
            self.server.close()
            loop.run_until_complete(self.server.wait_closed())
            Log.warn("Socks proxy server has been stopped.")
        else:
            Log.warn("Socks proxy server is not found.")

    def getServer(self):
        return self.server

    def accept_client(self, reader, writer):
        task = asyncio.Task(self.handle_client(reader, writer))
        clients[task] = (reader, writer)

        def client_finished(task):            
            #del clients[task]
            #writer.close()
            pass

        task.add_done_callback(client_finished)


    async def handle_client(self, reader, writer):
        timeout = 10
        client_peername = writer.get_extra_info('peername')
        Log.info("New Connection from: "+str(client_peername))
        while True:
            try:
                headBuffer = await asyncio.wait_for(reader.read(2),timeout=timeout)
            except:
                break
            if (headBuffer is None):
                continue
            else:
                if headBuffer == b'\x07\x02':
                    Log.debug('aaaaaaaaaaaaaaaaaaaaaaaaaaa')
                    cmd = await asyncio.wait_for(reader.read(1),timeout=timeout)
                    Log.debug('bbbbbbbbbbbbbbbbbbbbbbbbbbb')
                    Log.debug('cmd=='+str(cmd))
                    if (cmd == b'\x01'):
                        if (len(Config.config.password) > 0):                            
                            _len = await asyncio.wait_for(reader.read(1),timeout=timeout)
                            length = int.from_bytes(_len, byteorder='little')
                            _password = await asyncio.wait_for(reader.read(length),timeout=timeout)
                            password = _password.decode('utf-8')
                            if (password == Config.config.password):
                                #  Response Message
                                #   0   1   2    3
                                #  x06 x03 CMD STATUS
                                writer.write(b'\x06\x03\x02\x00')
                                await writer.drain()
                            else:
                                writer.write(b'\x06\x03\x02\x01')
                                await writer.drain()
                        else:
                            writer.write(b'\x06\x03\x02\x00')
                            await writer.drain()
                            Log.debug("Auth resposne has sent to local.")
                    elif (cmd == b'\x03'):
                        # Get contentLength
                        Log.debug('ccccccccccccccccccccccccccc')
                        b_length = await asyncio.wait_for(reader.read(2),timeout=timeout)
                        Log.debug('ddddddddddddddddddddddddddd')
                        contentLength = int.from_bytes(
                            b_length, byteorder='little')
                        Log.debug('eeeeeeeeeeeeeeeeeeeeeeeeeee')
                        b_type = await asyncio.wait_for(reader.read(1),timeout=timeout)
                        Log.debug('fffffffffffffffffffffffffff')
                        if (b_type == b'\x01'):
                            # process header
                            address = ""
                            for i in range(4):
                                address = address + str(int.from_bytes(await asyncio.wait_for(reader.read(1),timeout=timeout), byteorder='little'))                            
                            b_port = await asyncio.wait_for(reader.read(2),timeout=timeout)                            
                            port = int.from_bytes(b_port, byteorder='little')

                            # process content
                            b_content = await asyncio.wait_for(reader.read(contentLength),timeout=timeout)                            
                            await self.doproxy(reader=reader, writer=writer, address=address,
                                port=port, data=b_content, timeout=timeout)

                        elif(b_type == b'\x03'):
                            Log.info("Get contentLength: " +
                                str(contentLength))
                            # process header
                            Log.debug('ggggggggggggggggggggggggggggggg')
                            b_dLength = await asyncio.wait_for(reader.read(2),timeout=timeout)
                            Log.debug('hhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh')
                            domainLength = int.from_bytes(
                                b_dLength, byteorder='little')
                            Log.debug('jjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjj')
                            b_port = await asyncio.wait_for(reader.read(2),timeout=timeout)
                            Log.debug('kkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkk')
                            port = int.from_bytes(b_port, byteorder='little')
                            Log.debug('lllllllllllllllllllllllllllllllll')
                            b_domain = await asyncio.wait_for(reader.read(domainLength),timeout=timeout)
                            Log.debug('mmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmm')

                            # process content
                            Log.debug('nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn')
                            b_content = await asyncio.wait_for(reader.read(contentLength),timeout=timeout)
                            Log.debug('oooooooooooooooooooooooooooooooooo')
                            asyncio.Task(self.doproxy(reader=reader, writer=writer,
                                address=b_domain, port=port, data=b_content, timeout=timeout))

                        elif(b_type == b'\x05'):
                            # process header
                            address = ""
                            for i in range(4):
                                address = address + \
                                    binascii.b2a_hex(await asyncio.wait_for(reader.read(4),timeout=timeout))
                                if i != 3:
                                    address = address+":"
                            b_port = await asyncio.wait_for(reader.read(2),timeout=timeout)
                            port = int.from_bytes(b_port, byteorder='little')

                            # process content
                            b_content = await asyncio.wait_for(reader.read(contentLength),timeout=timeout)
                            self.doproxy(reader=reader, writer=writer, address=address,
                                port=port, data=b_content, timeout=timeout)


    async def doproxy(self, reader, writer, address, port, data, timeout):
        loop = asyncio.get_event_loop()
        addrs = await loop.getaddrinfo(host=address, port=port,type=socket.SOCK_STREAM)
        Log.debug('qqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqq')
        if len(addrs) == 0:
            Log.error('Get addrinfo failed.')
            return
        af, socktype, proto, canonname, sa = addrs[0]
        sock = socket.socket(af, socktype, proto)
        sock.setblocking(0)
        sock.setsockopt(socket.SOL_TCP, socket.TCP_NODELAY, 1)

        try:
            Log.debug('rrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr')
            await loop.sock_connect(sock, sa)
        except Exception as e:
            Log.error("Can't connect to remote: "+ str(e))
            return

        Log.debug('sssssssssssssssssssssssssssssssssssss')

        try:
            Log.debug('uuuuuuuuuuuuuuuuuuuuuuuuuuuuuu')
            sock.sendall(data)
            Log.debug('vvvvvvvvvvvvvvvvvvvvvvvvvvvvvv')
            Log.info("Data has sent to remote: " + str(data))
        except Exception as e:
            Log.error("An error occured when send data to remote: " + str(e))
            return

        header = b'\x06\x03\x04'
        Log.debug('ttttttttttttttttttttttttttttttttttttttt')
        sock.setblocking(0)
        while True:
            try:
                Log.debug('yyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy')
                recv = await loop.sock_recv(sock, 4096)
                asyncio.sleep(0.01)
            except Exception as e:
                break
            if not recv:
                break
            else:
                recv_len = len(recv)
                if (recv_len > 0):
                    Log.info("Data received: "+str(recv))
                    b_sendLen = recv_len.to_bytes(2, byteorder='little')
                    writer.write(header+b_sendLen+recv)
                    Log.debug('wwwwwwwwwwwwwwwwwwwwwwwwwwwwwww')
                    await writer.drain()
                    Log.debug('xxxxxxxxxxxxxxxxxxxxxxxxxxxxxx')
                    Log.debug("Data has sent to local")
                else:
                    break