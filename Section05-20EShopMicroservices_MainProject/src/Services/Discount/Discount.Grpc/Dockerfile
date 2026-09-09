FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["Services/Discount/Discount.Grpc/Discount.Grpc.csproj", "Services/Discount/Discount.Grpc/"]
RUN dotnet restore "Services/Discount/Discount.Grpc/Discount.Grpc.csproj"
COPY . .
WORKDIR "/src/Services/Discount/Discount.Grpc"
RUN dotnet build "Discount.Grpc.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "Discount.Grpc.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Discount.Grpc.dll"]