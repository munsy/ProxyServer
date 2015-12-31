# HTTP Proxy Server
A proxy server plays the “server” role with respect to a web browser, and the “client” role with the respect to a web server. It passes along requests from the client to the server. Proxy servers in common typically add value by acquiring a set of pages in a cache which they can serve to their clients faster than by going to the real server. It also acts as a way of filtering out advertising, animations, images, etc. from web pages. This proxy server simply passes every request along to the appropriate server and passes every response back to the client. It also logs all of the HTTP headers that it sees in both requests and responses, which helps make this part of the HTTP protocol more visible.

## Purpose
- To become familiar with basic programming techniques and patterns used in a multi-process or multi-threaded web server. 
- To further practice skills in creating TCP-based clients and servers. 
- To develop skills with regard to creating programs that simultaneously play the “client” and “server” role.
- To gain experience using string processing functions to parse and alter HTTP headers.

As a consequence of that last note, we were not allowed to use language-supplied or 3rd party libraries for protocol and server creation. Instead, we had to write all of the necessary protocol processing code ourselvse. One of the objectives of the project was to become familiar with the details of the protocols and how servers are constructed, so using language or 3rd party libraries would trivialize this objective.

## Specifications
While running, the proxy server does the following sequence of actions:

1. Accepts connections from web clients (i.e. Firefox or Internet Explorer) on a port of your choosing.
2. Upon accepting a connection creates a separate thread to handle communication for that request using a forking TCP server pattern.
3. The thread reads an HTTP request from the socket connected to the client.
4. The thread creates a new socket connected to the server specified in the client’s request.
5. The thread passes an optionally-modified version of the client’s request to the server.
6. The thread reads the server’s response and passes an optionally-modified version of it to the client.

The thread handling a request will print the request and response messages, along with all HTTP headers found in both the request and the response (before any alterations performed by the proxy server), on the standard output of the proxy server. 

## References
- [Wikipedia - Chunked Transfer Encoding](https://en.wikipedia.org/wiki/Chunked_transfer_encoding)
- [RFC 7230 - Hypertext Transfer Protocol (HTTP/1.1): Message Syntax and Routing](https://tools.ietf.org/html/rfc7230)

## Co-Authors
- Justin Slone
- Alex Kerr
- Tim Munson
