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

    def __init__(self, host, port):
        self.host = host
        self.port = port
        self.server = None
        self.coro = None

    def start(self, loop):
        self.coro = asyncio.start_server(
            self.accept_client, self.host, self.port, loop=loop)
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

    def accept_client(self, reader, writer):
        task = asyncio.Task(self.handle_client(reader, writer))
        clients[task] = (reader, writer)

        def client_finished(task):
            del clients[task]
            writer.close()
            pass

        task.add_done_callback(client_finished)

    async def handle_client(self, reader, writer):
        timeout = 15
        client_peername = writer.get_extra_info('peername')
        Log.info("New Connection from: "+str(client_peername))
        remote = None
        loop = asyncio.get_event_loop()
        while True:
            try:
                headBuffer = await asyncio.wait_for(reader.read(2), timeout=timeout)
            except:
                break
            if (headBuffer is None):
                continue
            else:
                if headBuffer == b'\x07\x02':
                    cmd = await asyncio.wait_for(reader.read(1), timeout=timeout)
                    Log.debug('cmd=='+str(cmd))
                    if (cmd == b'\x01'):
                        if (len(Config.config.password) > 0):
                            _len = await asyncio.wait_for(reader.read(1), timeout=timeout)
                            length = int.from_bytes(_len, byteorder='little')
                            _password = await asyncio.wait_for(reader.read(length), timeout=timeout)
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
                    elif (cmd == b'\x02'):
                        address=None
                        port=None
                        b_type = await asyncio.wait_for(reader.read(1), timeout=timeout)
                        if (b_type == b'\x01'):
                            # process header
                            address = ""
                            for i in range(4):
                                address = address + str(int.from_bytes(await asyncio.wait_for(reader.read(1), timeout=timeout), byteorder='little'))
                                if (i != 3):
                                    address = address+'.'
                            b_port = await asyncio.wait_for(reader.read(2), timeout=timeout)
                            port = int.from_bytes(b_port, byteorder='little')
                        elif(b_type == b'\x03'):
                            # process header
                            b_dLength = await asyncio.wait_for(reader.read(2), timeout=timeout)
                            domainLength = int.from_bytes(
                                b_dLength, byteorder='little')
                            b_port = await asyncio.wait_for(reader.read(2), timeout=timeout)
                            port = int.from_bytes(b_port, byteorder='little')
                            address = await asyncio.wait_for(reader.read(domainLength), timeout=timeout)
                        elif(b_type == b'\x04'):
                            # process header
                            address = ""
                            for i in range(4):
                                address = address + \
                                    binascii.b2a_hex(await asyncio.wait_for(reader.read(4), timeout=timeout))
                                if i != 3:
                                    address = address+":"
                            b_port = await asyncio.wait_for(reader.read(2), timeout=timeout)
                            port = int.from_bytes(b_port, byteorder='little')

                        addrs = await loop.getaddrinfo(host=address, port=port)
                        if len(addrs) == 0:
                            Log.error('Get addrinfo failed.')
                            return
                        af, socktype, proto, canonname, sa = addrs[0]
                        remote = socket.socket(af, socktype, proto)
                        remote.setblocking(0)
                        try:
                            await loop.sock_connect(remote, sa)
                        except Exception as e:
                            Log.error("Can't connect to remote: " + str(e))
                            return
                    elif (cmd == b'\x03'):
                        # Get contentLength
                        b_length = await asyncio.wait_for(reader.read(2), timeout=timeout)
                        contentLength = int.from_bytes(
                            b_length, byteorder='little')
                        b_content = await asyncio.wait_for(reader.read(contentLength), timeout=timeout)
                        asyncio.Task(self.doproxy(remote=remote, reader=reader, writer=writer, address=address, port=port, data=b_content, timeout=timeout))

    async def doproxy(self,remote, reader, writer, address, port, data, timeout):
        header = b'\x06\x03\x04'
        loop = asyncio.get_event_loop()

        try:
            await loop.sock_sendall(remote, data)
            Log.info("Data has sent to remote: " + str(data))
        except Exception as e:
            Log.error("An error occured when send data to remote: " + str(e))
            return

        while True:
            try:
                recv = await loop.sock_recv(remote, 4096)
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
                    await writer.drain()
                    Log.debug("Data has sent to local")
                else:
                    break
