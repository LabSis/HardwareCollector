#!/usr/bin/env python3
#encoding: UTF-8

if __name__ == "__main__":
    from pprint import pprint
    import recoleccion.collector as collector
    from util.controladorarchivoconfiguracion import ControladorArchivoConfiguracion
    from constantes import *
    from conexion.cliente import Cliente
    from conexion.comando import Comando
    from conexion.comando_configurar import ComandoConfigurar
    from conexion.comando_informar import ComandoInformar
    from conexion.comando_inicio import ComandoInicio
    from conexion.comando_maquina_nueva import ComandoMaquinaNueva
    from conexion.comando_maquina_registrada import ComandoMaquinaRegistrada
    from conexion.comando_reportar import ComandoReportar
    from conexion.comando_solicitar import ComandoSolicitar

if (ControladorArchivoConfiguracion.existe_archivo()):
    archivo = ControladorArchivoConfiguracion.leer_archivo()        
    cliente = Cliente()
    cliente.set_ip_servidor(archivo.getconfiguracion().getservidor().getip())
    cliente.set_puerto(archivo.getconfiguracion().getservidor().getpuerto())        
    #cliente.conectar() #cliente inicia socket con servidor. DESCOMENTAR
    if (not archivo.posee_id()):
        #enviar comando máquina nueva, recibir id, actualizar archivo
        cmd_maquina_nueva = ComandoMaquinaNueva()
        cliente.enviar_comando(cmd_maquina_nueva)
        cmd_maquina_registrada = cliente.recibir_comando()
        id_maquina_registrada = cmd_maquina_registrada.get_datos().get_id()
        archivo.setid(id_maquina_registrada)
        ControladorArchivoConfiguracion.escribir_archivo(archivo)
        cmd_inicio = ComandoInicio()
        datos_cmd_inicio = ComandoInicio().Datos()
        datos_cmd_inicio.set_id(archivo.getid())
        cmd_inicio.set_datos(datos_cmd_inicio)
        cliente.enviar_comando(cmd_inicio)
        #ya está en funcionamiento
    else:
        #Ya tiene asignada IP y comienza a trabajar
        cmd_inicio = ComandoInicio()
        datos_cmd_inicio = ComandoInicio().Datos()
        datos_cmd_inicio.set_id(archivo.getid())
        cmd_inicio.set_datos(datos_cmd_inicio)
        cliente.enviar_comando(cmd_inicio)
        #ya está en funcionamiento    
    while (True):
        print("EN FUNCIONAMIENTO")
        cmd = cliente.recibir_comando()
#        print(vars(cmd))
        if (type(cmd) is ComandoConfigurar):
            print("RECIBO COMANDO CONFIGURAR, CONFIGURACIÓN RECIBIDA");
            #piso la configuracion actual, solo informes, del cliente.
            cmd_configurar = cmd
            archivo.setconfiguracion(cmd_configurar.get_datos().get_configuracion().getconfiguracion())
            ControladorArchivoConfiguracion.escribir_archivo(archivo)
        elif (type(cmd) is ComandoSolicitar):
            print("RECIBO COMANDO SOLICITAR, ENVÍO DE INFORME");
            cmd_solicitar = cmd
            cmd_informar = ComandoInformar()
            cmd_informar.datos.set_id_solicitud(cmd_solicitar.get_datos().get_id_solicitud())
            informacion_solicitada = cmd_solicitar.get_datos().get_informacion()
            informacion_informar = [] # ComandoInformar.ElementoInfomacion
            recolector = collector.Collector()
            maquina = recolector.get_maquina()
#            print(vars(maquina.getprocesador()))
            for i in range(0,len(informacion_solicitada)):
                if informacion_solicitada[i] == PROCESADOR:
                    procesador = maquina.getprocesador()
                    elemento_procesador = ComandoInformar.ElementoProcesador()
                    elemento_procesador.get_datos().actualizar_datos(procesador)
                    informacion_informar.append(elemento_procesador)
                elif informacion_solicitada[i] == MEMORIAS_RAM:
                    memorias_ram = maquina.getmemoriasram()
                    elemento_memorias_ram = ComandoInformar.ElementoMemoriasRam()
                    datos_memorias = []
                    for i in range(0,len(memorias_ram)):
                        memoria_datos = ComandoInformar.DatosInformacionMemoriasRam()
                        memoria_datos.actualizar_datos(memorias_ram[i])
                        datos_memorias.append(memoria_datos)
                    elemento_memorias_ram.set_datos(datos_memorias)
                    informacion_informar.append(elemento_memorias_ram)
                elif informacion_solicitada[i] == DISCOS_DUROS:
                    discos_duros = maquina.getdiscosduros()
                    elemento_discos_duros = ComandoInformar.ElementoDiscosDuros()
                    datos_discos = []
                    for i in range(0,len(discos_duros)):
                        disco_datos = ComandoInformar.DatosInformacionDiscosDuros()
                        disco_datos.actualizar_datos(discos_duros[i])
                        datos_discos.append(disco_datos)
                    elemento_discos_duros.set_datos(datos_discos)
                    informacion_informar.append(elemento_discos_duros)
            cmd_informar.datos.set_informacion(informacion_informar)
            cliente.enviar_comando(cmd_informar)
else:
    #si o si debe existir el archivo con la direccion del servidor, sino no puede contectarse...
    #termina
    print("No existe el archivo de configuracion, no es posible hallar el servidor")