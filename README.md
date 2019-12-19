# Courier
Courier is a client/server application for transferring files over a serial connection between two computer. The server listens for requests from the client, and performs the requested operation (send a file, receive a file, list files, etc.)

The primary application of Courier is to allow files transfer between an air-gapped computer (with the exception of a serial link) and a computer attached to the local network. 

For an air-gapped computer, moving any data on or off of the computer presents a risk. Using removable media such as a CD or USB drive presents an path for malware to move undetected onto the air-gapped computer. Additionally, data could be exfiltrated from the air-gapped computer over multiple hops between it and a network-attached computer.

Serial ports are relatively simple compared to modern means of conectivity, and would be especially resistent to attack for all but a specifically targetted attack on the server. The serial link must be created manually from the client (running on the air-gapped computer), and all communication will be ignored from the server until the link is established by the hardware itself.

Using Courier, a serial connection can provide a reliable way of moving data to and from an air-gapped computer with minimal risk when compared to other forms of data transmission.
