namespace RinhaDeBackend.Cache
{
    public static class ClientesCache
    {
        static Dictionary<int, int> dicionarioClientes = new Dictionary<int, int>(){
            {1, 1000 * 100},
            {2, 800 * 100},
            {3, 10000 * 100},
            {4, 100000 * 100},
            {5, 5000 * 100},
        };

        public static int ObterLimiteCliente(int clienteId)
        {
            if (dicionarioClientes.TryGetValue(clienteId, out int limite))
            {
                return limite;
            }
            else
            {
                return 0;
            }
        }
    }
}