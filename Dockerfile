FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilacao
ARG PROJETO
ARG CONFIGURACAO=Release
WORKDIR /codigo

COPY Directory.Build.props Directory.Packages.props ./
COPY src/ src/

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet restore "$PROJETO"

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet publish "$PROJETO" --configuration "$CONFIGURACAO" --no-restore --output /publicacao /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS execucao
ARG ASSEMBLY
ENV ASSEMBLY_DO_SERVICO=${ASSEMBLY}.dll
WORKDIR /app
COPY --from=compilacao /publicacao .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet \"$ASSEMBLY_DO_SERVICO\""]
