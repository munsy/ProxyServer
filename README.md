# HTTP Proxy Server
A proxy server plays the “server” role with respect to a web browser, and the “client” role with the respect to a web server. It passes along requests from the client to the server. Proxy servers in common typically add value by acquiring a set of pages in a cache which they can serve to their clients faster than by going to the real server. It also acts as a way of filtering out advertising, animations, images, etc. from web pages. This proxy server simply passes every request along to the appropriate server and passes every response back to the client. It also logs all of the HTTP headers that it sees in both requests and responses, which helps make this part of the HTTP protocol more visible.

## Purpose
- To become familiar with basic programming techniques and patterns used in a multi-process or multi-threaded web server. 
- To further practice skills in creating TCP-based clients and servers. 
- To develop skills with regard to creating programs that simultaneously play the “client” and “server” role.
- To gain experience using string processing functions to parse and alter HTTP headers.

As a consequence of that last note, we were not allowed to use language-supplied or 3rd party libraries for protocol and server creation. Instead, we had to write all of the necessary protocol processing code ourselvse. One of the objectives of the project was to become familiar with the details of the protocols and how servers are constructed. Using language or 3rd party libraries would trivialize this objective.



## References
- [Wikipedia - Chunked Transfer Encoding](https://en.wikipedia.org/wiki/Chunked_transfer_encoding)
- [RFC 7230 - Hypertext Transfer Protocol (HTTP/1.1): Message Syntax and Routing](https://tools.ietf.org/html/rfc7230)

## Co-Authors
- Justin Slone
- Alex Kerr
- Tim Munson
