def GetMethod(packet):
    return packet.split(' ',1)[0]

def GetFirstLine(packet):
    firstLine = packet.split('\r\n',1)[0]
    return firstLine.split(' ')

def GetHost(packet):
    lines = packet.split('\r\n')
    for line in lines:
        if 'Host:' in line:
            return line.split(' ')[1]
    return None