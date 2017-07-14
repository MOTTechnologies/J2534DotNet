# PHF2BIN
Extremely basic application that converts a ford PHF file to a binary image. 
Currently is hard coded to convert spanish oak 1024k and black oak 1472k.
Program could easily be expanded to be more generic and convert other files.

PHF sections that are not defined are padded with 0xFF in the resultant binary. Sections 0x8000 -> 0x10000 are missing from the PHF file
I suspect these sections are for the RTOS/OBD/UDS command handler and flash bootstrap code.

Providing you write address 0x10000 onwards the resultant file could be written to a vehicle, the VIN however will be left blank meaning PATS will kick in causing the vehicle to not start.
