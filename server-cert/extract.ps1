docker run --name asb-server-cert asb-server-cert
docker cp asb-server-cert:/home/testca .
docker rm -f asb-server-cert