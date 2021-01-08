# Self-Driving-Rides

This is the source code of the solver developed within the course Nature-Inspired Algorithms to solve the problem of self driving rides 
published in the competition [Google Hash Code 2018](https://codingcompetitions.withgoogle.com/hashcode/archive/2018).
We solved this problem using the guided local search algorithm combined with the fast local search algorithm with penalty reduction.

# Building

[.NET Framework](https://dotnet.microsoft.com/download/dotnet-framework/net472) (4.7.2 or higher) is required to build the solver.

To build standalone `.exe` file after cloning the repository run the following commands:
```console
> cd ../SelfDrivingRides/SelfDrivingRides
> csc Program.cs Ride.cs Car.cs RideQuality.cs -out:Solver.exe
```
Output `.exe` file will be written in the same directory under the name `Solver` 

# Running

The executable file `Solver.exe` accepts the following arguments:
```console
> Solver.exe <instance file path> <solved output save path>

OPTIONS

<instance file path>          The path of the input instance problem
<solved output save path>     The path where you save the solution
```

# License

MIT License
