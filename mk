#!/bin/sh

if [ -e ./HTTP_Proxy_Server/Program.exe ]
	then
		rm ./HTTP_Proxy_Server/Program.exe
fi

mcs ./HTTP_Proxy_Server/Program.cs

./HTTP_Proxy_Server/Program.exe 127.0.0.1 45500
