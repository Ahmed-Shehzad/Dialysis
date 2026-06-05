import {
  HubConnectionBuilder,
  HubConnection,
  HttpTransportType,
  LogLevel,
} from "@microsoft/signalr";

export type SignalrFactoryOptions = {
  url: string;
  accessTokenFactory: () => string | null | Promise<string | null>;
};

export const buildHubConnection = ({
  url,
  accessTokenFactory,
}: SignalrFactoryOptions): HubConnection =>
  new HubConnectionBuilder()
    .withUrl(url, {
      accessTokenFactory: async () => (await accessTokenFactory()) ?? "",
      transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling,
    })
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: (ctx) =>
        Math.min(30_000, 1_000 * 2 ** Math.min(ctx.previousRetryCount, 5)),
    })
    .configureLogging(LogLevel.Warning)
    .build();
