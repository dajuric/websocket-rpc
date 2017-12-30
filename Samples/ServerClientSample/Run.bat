set serverApp = "Server\bin\Server.exe"
set clientApp = "Client\bin\Client.exe"

if NOT EXIST %serverApp% (
  echo Build 'Server' project first.
  goto :eof
)

if NOT EXIST %clientApp% (
  echo Build 'Client' project first.
  goto :eof
)

start %serverApp%
timeout 1>nul
start %clientApp%