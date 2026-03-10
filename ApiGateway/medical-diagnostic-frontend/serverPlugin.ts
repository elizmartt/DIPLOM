export default () => ({
    name: 'server',
    configureServer(server: any) {
        return () => {
            server.printUrls = () => {};
        };
    },
});