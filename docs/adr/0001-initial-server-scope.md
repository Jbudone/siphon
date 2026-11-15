# Initial server scope

Status: accepted for the first implementation.

One authoritative dedicated server owns one persistent war with up to 32 human players. Bots have a separate measured budget. World regions organize spatial queries and simulation detail; they are not separate servers.

This avoids cross-server authority transfers and combat boundaries while we validate fast movement. Future scale requires evidence from representative core assaults, bot workloads, structures, and bandwidth. Merely enlarging the map does not establish the need for additional servers.

Persistence must explicitly serialize world state and simulation time. Full and coarse simulations should share domain rules. No production networking or persistence package has been selected yet.
