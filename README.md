DS3 SaveBackup
======
Steam deck plugin - DS3 SaveBackup - OneDrive plugin.

Plugin starts up the backend and communicate with it via unix socket.


## Protocol
Python as server, starts up a unix socket as `/tmp/ds3savebackup.sock`, wait for connection.

```json
{
    "register": {
        "name": "onedrive"
    }
}
```


```json
{
    "command": "GetAccounts"
}
```
