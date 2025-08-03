# Camille

Camille aims to be an XMPP server that tries to implement enough of the standard to establish 
and maintain an active connection with a pre-existing, third-party, Client. As this server 
interfaces with an existing client, the hope is that the server will be forced to implement 
key features of that standard that would allow for the server to be a general purpose XMPP server.

I have absolutely 0 idea on how XMPP works other than it uses XML, and I have basically 0 
experience creating anything like this. It will be interesting to see if I can actually do this 
:)

## Security

Not that great to be honest with you. The library that is used by the third-party client 
is "[as3crypto](https://code.google.com/archive/p/as3crypto/)", an Actionscript3 
cryptography library that was stated to have "partial TLS 1.0 support" back when TLSv1.2 
was just one month away from releasing to the public. As a result, this project actively 
requires deprecated functionality and often ignores clearly stated security warnings. 


Could this be improved? Probably yeah, I'm just not focused on it since I don't actually 
have a way to test it due to a lack of support from the client I am using. The as3-crypto 
library could be theoretically updated to support TLSv1.3; that one will 
probably remain on the backburner till atleast this project is done.

## Name?

League of Legends reference. There is a character called Camille that has blades for legs, and 
this project is C# so like the C is Camille and the Sharp is her legs? Probably sounds like a 
stretch because it is, I picked the name whilst debating over picking between C++/Java.
