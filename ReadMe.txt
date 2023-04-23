webapi must only directly communicate with api service, which will in turn communicate with runninggameservice

signalr must only communicate directly with group service, which will in turn communicate with running game service

this allows validation in middle steps