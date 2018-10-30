import datetime
from queue import Queue

# -----------------colorama模块的一些常量---------------------------
# Fore: BLACK, RED, GREEN, YELLOW, BLUE, MAGENTA, CYAN, WHITE, RESET.
# Back: BLACK, RED, GREEN, YELLOW, BLUE, MAGENTA, CYAN, WHITE, RESET.
# Style: DIM, NORMAL, BRIGHT, RESET_ALL
#
from colorama import init, Fore, Back, Style
init(autoreset=True)
class Colored(object):
    def red(self, s):
        return Fore.RED + s + Fore.RESET
    def green(self, s):
        return Fore.GREEN + s + Fore.RESET
    def yellow(self, s):
        return Fore.YELLOW + s + Fore.RESET
    def blue(self, s):
        return Fore.BLUE + s + Fore.RESET
    def magenta(self, s):
        return Fore.MAGENTA + s + Fore.RESET
    def cyan(self, s):
        return Fore.CYAN + s + Fore.RESET
    def white(self, s):
        return Fore.WHITE + s + Fore.RESET
    def black(self, s):
        return Fore.BLACK
    def white_green(self, s):
        return Fore.WHITE + Back.GREEN + s + Fore.RESET + Back.RESET
color = Colored()

def info(content):
    nowtime = datetime.datetime.strftime(datetime.datetime.now(),'%Y-%m-%d %H:%M:%S')
    print(color.green("[Info]  "+nowtime+":\t"+content),end="\r\n")

def debug(content):
    nowtime = datetime.datetime.strftime(datetime.datetime.now(),'%Y-%m-%d %H:%M:%S')
    print(color.white("[Debug] "+nowtime+":\t"+content),end="\r\n")

def warn(content):
    nowtime = datetime.datetime.strftime(datetime.datetime.now(),'%Y-%m-%d %H:%M:%S')
    print(color.yellow("[Warn]  "+nowtime+":\t"+content),end="\r\n")

def error(content):
    nowtime = datetime.datetime.strftime(datetime.datetime.now(),'%Y-%m-%d %H:%M:%S')
    print(color.red("[Error] "+nowtime+":\t"+content),end="\r\n")