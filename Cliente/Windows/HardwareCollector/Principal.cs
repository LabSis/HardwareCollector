﻿using System;
using System.Management;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using HardwareCollector.Recoleccion;
using HardwareCollector.Componente;
using HardwareCollector.Conexion;
using HardwareCollector.Util;
using System.Net.Sockets;
using System.IO;

namespace HardwareCollector
{
    class Principal
    {

        //https://msdn.microsoft.com/en-us/library/aa390887(v=vs.85).aspx
        public static void Main()
        {
            try
            {
                if (ControladorArchivoConfiguracion.ExisteArchivo())
                {
                    ArchivoConfiguracion archivo = ControladorArchivoConfiguracion.LeerArchivo();
                    //si es el archivo no tiene la direccion del servidor, termina...
                    if (ControladorArchivoConfiguracion.PoseeIpValida(archivo) && ControladorArchivoConfiguracion.PoseePuertoValido(archivo))
                    {
                        Cliente cliente = new Cliente();
                        cliente.ipServidor = archivo.configuracion.servidor.ip;
                        cliente.puertoServidor = archivo.configuracion.servidor.puerto;
                        cliente.conectar();
                        Console.WriteLine("CONECTADO");
                        //si no encuentra su id, le solicita al servidor
                        if (!archivo.PoseeId())
                        {
                            Console.WriteLine("ENVIO COMANDO MAQUINA NUEVA - SOLICITANDO ID");
                            cliente.enviarComando(new ComandoMaquinaNueva());
                            ComandoMaquinaRegistrada comandoMaquinaRegistrada = ((ComandoMaquinaRegistrada)cliente.recibirComando());
                            string id = comandoMaquinaRegistrada.datos.id;
                            archivo.id = id;
                            Console.WriteLine("COMANDO MAQUINA REGISTRADA RECIBIDO, ID MAQUINA: " + id);
                            ControladorArchivoConfiguracion.EscribirArchivo(archivo);
                            archivo = ControladorArchivoConfiguracion.LeerArchivo();
                            ComandoInicio comandoInicio = new ComandoInicio();
                            comandoInicio.datos.id = archivo.id;
                            Console.WriteLine("ENVIO COMANDO INICIO SOLICTANDO CONFIGURACION");
                            cliente.enviarComando(comandoInicio);
                            //necesito configuracion
                            //espero el mensaje del servidor
                            ComandoConfigurar comandoConfigurar = ((ComandoConfigurar)cliente.recibirComando());
                            archivo.configuracion.informes = ((ArchivoConfiguracion)comandoConfigurar.datos.configuracion).configuracion.informes;
                            ControladorArchivoConfiguracion.EscribirArchivo(archivo);
                            Console.WriteLine("RECIBO COMANDO CONFIGURAR, CONFIGURACION RECIBIDA");
                        }
                        else
                        {
                            //aviso que me conecte y puedo empezar a trabajar
                            Console.WriteLine("ENVIO COMANDO INICIO SOLICTANDO CONFIGURACION");
                            ComandoInicio comandoInicio = new ComandoInicio();
                            comandoInicio.datos.id = archivo.id;
                            cliente.enviarComando(comandoInicio);
                        }
                        //...
                        //estoy en funcionamiento


                        while (true)
                        {
                            Console.WriteLine("EN FUNCIONAMIENTO");
                            Comando comando = cliente.recibirComando();
                            if (comando is ComandoConfigurar)
                            {
                                Console.WriteLine("RECIBO COMANDO CONFIGURAR, CONFIGURACION RECIBIDA");
                                //piso la configuracion actual del cliente, podria solo pisar la parte de informes (y no la id cliente y datos del servidor)
                                ComandoConfigurar comandoConfigurar = (ComandoConfigurar)comando;
                                archivo.configuracion.informes = ((ArchivoConfiguracion)comandoConfigurar.datos.configuracion).configuracion.informes;
                                ControladorArchivoConfiguracion.EscribirArchivo(archivo);
                            }
                            else if (comando is ComandoSolicitar)
                            {
                                Console.WriteLine("RECIBO COMANDO SOLICITAR");
                                ComandoSolicitar comandoSolicitar = (ComandoSolicitar)comando;
                                ComandoInformar comandoInformar = new ComandoInformar();
                                Console.WriteLine(comandoSolicitar.datos.id_solicitud);
                                comandoInformar.datos.id_solicitud = comandoSolicitar.datos.id_solicitud;
                                Console.WriteLine(comandoSolicitar.datos.informacion[0]);
                                List<ComandoInformar.ElementoInformacion> informacionInformar = new List<ComandoInformar.ElementoInformacion>();
                                List<string> informacionSolicitada = comandoSolicitar.datos.informacion;
                                Recolector recolector = new Recolector();
                                //por defecto pido toda la maquina, es ineficiente
                                Maquina maquina = recolector.GetMaquina();
                                for (int i = 0; i < informacionSolicitada.Count; i++)
                                {
                                    if (informacionSolicitada[i] == "procesador")
                                    {
                                        Procesador procesador = maquina.Procesador;
                                        ComandoInformar.ElementoProcesador elementoProcesador = new ComandoInformar.ElementoProcesador();
                                        ((ComandoInformar.DatosInformacionProcesador)elementoProcesador.datos).nombre = procesador.Nombre;
                                        ((ComandoInformar.DatosInformacionProcesador)elementoProcesador.datos).descripcion = procesador.Descripcion;
                                        ((ComandoInformar.DatosInformacionProcesador)elementoProcesador.datos).fabricante = procesador.Fabricante;
                                        ((ComandoInformar.DatosInformacionProcesador)elementoProcesador.datos).arquitectura = procesador.Arquitectura;
                                        ((ComandoInformar.DatosInformacionProcesador)elementoProcesador.datos).cantidad_nucleos = procesador.CantidadNucleos;
                                        ((ComandoInformar.DatosInformacionProcesador)elementoProcesador.datos).velocidad = procesador.Velocidad.ToString();
                                        ((ComandoInformar.DatosInformacionProcesador)elementoProcesador.datos).tamanio_cache = procesador.Cache.ToString();
                                        informacionInformar.Add(elementoProcesador);
                                    }
                                    else if (informacionSolicitada[i] == "memorias_ram")
                                    {
                                        List<MemoriaRam> memorias = maquina.MemoriasRam;
                                        ComandoInformar.ElementoMemoria elementoMemoria = new ComandoInformar.ElementoMemoria();
                                        foreach (MemoriaRam memoria in memorias)
                                        {
                                            ComandoInformar.DatosInformacionMemoriasRam dato = new ComandoInformar.DatosInformacionMemoriasRam();
                                            dato.banco = memoria.Banco;
                                            dato.tecnologia = memoria.Tecnologia;
                                            dato.fabricante = memoria.Fabricante;
                                            dato.numero_serie = memoria.NumeroSerie;
                                            dato.tamanio_bus_datos = memoria.TamanioBusDatos;
                                            dato.velocidad = memoria.Velocidad.ToString();
                                            dato.tamanio = memoria.Capacidad.ToString();
                                            elementoMemoria.datos.Add(dato);
                                        }
                                        informacionInformar.Add(elementoMemoria);
                                    }
                                    else if (informacionSolicitada[i] == "discos_duros")
                                    {
                                        List<DiscoDuro> discos = maquina.DiscosDuros;
                                        ComandoInformar.ElementoDiscoDuro elemento = new ComandoInformar.ElementoDiscoDuro();
                                        foreach (DiscoDuro disco in discos)
                                        {
                                            ComandoInformar.DatosInformacionDiscosDuros dato = new ComandoInformar.DatosInformacionDiscosDuros();
                                            dato.fabricante = disco.Fabricante;
                                            dato.modelo = disco.Modelo;
                                            dato.numero_serie = disco.NumeroSerie;
                                            dato.tipo_interfaz = disco.TipoInterfaz;
                                            dato.firmware = disco.Firmware;
                                            dato.cantidad_particiones = disco.CantidadParticiones;
                                            dato.tamanio = disco.Capacidad.ToString();
                                            elemento.datos.Add(dato);
                                        }
                                        informacionInformar.Add(elemento);
                                    }
                                }
                                comandoInformar.datos.informacion = informacionInformar;
                                Console.WriteLine("ENVIO COMANDO INFORMAR");
                                cliente.enviarComando(comandoInformar);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("El archivo de configuración existe pero no posee ip y/o puerto válido.");
                    }
                }
                else
                {
                    //si o si debe existir el archivo con la direccion del servidor, sino no puede contectarse...
                    Console.WriteLine("No existe el archivo de configuración, no es posible hallar el servidor.");
                }
            }
            catch (SocketException se)
            {
                //10061:  servidor no encontrado
                if (se.ErrorCode.Equals(10061))
                {
                    Console.WriteLine("Servidor apagado.");
                }
                else
                {
                    Console.WriteLine("Error en el socket: {0}", se.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.ToString());
            }
            Console.ReadLine();
        }

        private static void ConsultarDatosClaseWMI(String NombreClase)
        {
            SelectQuery selectQuery = new SelectQuery("SELECT * FROM " + NombreClase);
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(selectQuery);
            foreach (ManagementObject managementObject in searcher.Get())
            {
                foreach (PropertyData propiedad in managementObject.Properties)
                {
                    Console.WriteLine(propiedad.Name + ": " + managementObject[propiedad.Name]);
                }
            }
        }
    }

}
