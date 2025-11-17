using System;
using System.Collections.Generic;
using Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public partial class TallerMecanicoContext : DbContext
{
    public TallerMecanicoContext(DbContextOptions<TallerMecanicoContext> options)
        : base(options)
    {
    }

    public virtual DbSet<asignacion_talleres> asignacion_talleres { get; set; }

    public virtual DbSet<taller_cliente> taller_cliente { get; set; }

    public virtual DbSet<atencion> atencion { get; set; }

    public virtual DbSet<bitacora> bitacora { get; set; }

    public virtual DbSet<agenda> agenda { get; set; }

    public virtual DbSet<categorium> categoria { get; set; }

    public virtual DbSet<cliente> cliente { get; set; }

    public virtual DbSet<cliente_vehiculo> cliente_vehiculo { get; set; }

    public virtual DbSet<comuna> comuna { get; set; }

    public virtual DbSet<cotizacion> cotizacion { get; set; }

    public virtual DbSet<factura> factura { get; set; }

    public virtual DbSet<funcionario> funcionario { get; set; }

    public virtual DbSet<log_inventario> log_inventario { get; set; }

    public virtual DbSet<region> region { get; set; }

    public virtual DbSet<repuesto> repuesto { get; set; }

    public virtual DbSet<repuesto_unidades> repuesto_unidades { get; set; }

    public virtual DbSet<servicio> servicio { get; set; }

    public virtual DbSet<taller> taller { get; set; }

    public virtual DbSet<taller_repuesto> taller_repuesto { get; set; }

    public virtual DbSet<tipo_funcionario> tipo_funcionario { get; set; }

    public virtual DbSet<tipo_servicio> tipo_servicio { get; set; }

    public virtual DbSet<servicio_repuesto> servicio_repuesto { get; set; }

    public virtual DbSet<tipo_vehiculo> tipo_vehiculo { get; set; }

    public virtual DbSet<usuario> usuario { get; set; }

    public virtual DbSet<vehiculo> vehiculo { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<asignacion_talleres>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__asignaci__3213E83F266FB600");

            entity.HasOne(d => d.funcionario).WithMany(p => p.asignacion_talleres)
                .HasForeignKey(d => d.funcionario_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_asignacion_funcionario");

            entity.HasOne(d => d.taller).WithMany(p => p.asignacion_talleres)
                .HasForeignKey(d => d.taller_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_asignacion_taller");

            entity.Property(e => e.ultimo_activo)
                .HasDefaultValue(false);
        });

        modelBuilder.Entity<taller_cliente>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__taller_c__3213E83F40DC9C2B");
            entity.HasOne(d => d.taller).WithMany(p => p.taller_clientes)
                .HasForeignKey(d => d.taller_id)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_taller_cliente_taller");
            entity.HasOne(d => d.cliente).WithMany(p => p.taller_clientes)
                .HasForeignKey(d => d.cliente_id)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_taller_cliente_cliente");
        });

        modelBuilder.Entity<atencion>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__atencion__3213E83FBEAD1484");

            entity.ToTable("atencion");

            entity.Property(e => e.estado)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Pendiente");
            entity.Property(e => e.fecha_ingreso)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.observaciones)
                .HasMaxLength(1000)
                .IsUnicode(false);

            entity.HasOne(d => d.administrativo).WithMany(p => p.atencionadministrativos)
                .HasForeignKey(d => d.administrativo_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_atencion_administrativo");

            entity.HasOne(d => d.cliente).WithMany(p => p.atencions)
                .HasForeignKey(d => d.cliente_id)
                .HasConstraintName("fk_atencion_cliente");

            entity.HasOne(d => d.cotizacion).WithMany(p => p.atencions)
                .HasForeignKey(d => d.cotizacion_id)
                .HasConstraintName("fk_atencion_cotizacion");

            entity.HasOne(d => d.mecanico).WithMany(p => p.atencionmecanicos)
                .HasForeignKey(d => d.mecanico_id)
                .HasConstraintName("fk_atencion_mecanico");

            entity.HasOne(d => d.taller).WithMany(p => p.atencions)
                .HasForeignKey(d => d.taller_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_atencion_taller");

            entity.HasOne(d => d.agenda)
                .WithOne(p => p.atencion)
                .HasForeignKey<atencion>(d => d.agenda_id)
                .HasConstraintName("fk_atencion_agenda");

            entity.HasOne(d => d.vehiculo).WithMany(p => p.atencions)
                .HasForeignKey(d => d.vehiculo_id)
                .HasConstraintName("fk_atencion_vehiculo");
        });

        modelBuilder.Entity<agenda>(entity =>
        {
            entity.ToTable("agenda");
            entity.HasKey(e => e.id);
            entity.Property(e => e.id).UseIdentityColumn();

            entity.Property(e => e.titulo)
                  .IsRequired()
                  .HasMaxLength(50);
            entity.Property(e => e.fecha_creacion)
                  .IsRequired()
                  .HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.fecha_agenda)
                  .IsRequired();
            entity.Property(e => e.estado).HasMaxLength(20);
            entity.Property(e => e.comentarios).HasMaxLength(200);
        });

        modelBuilder.Entity<bitacora>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__bitacora__3213E83FA16FBD5C");

            entity.ToTable("bitacora");

            entity.Property(e => e.created_at)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.descripcion)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.estado)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.tipo)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.atencion).WithMany(p => p.bitacoras)
                .HasForeignKey(d => d.atencion_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_bitacora_atencion");
        });

        modelBuilder.Entity<categorium>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__categori__3213E83F17793490");

            entity.Property(e => e.descripcion)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.nombre)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<cliente>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__cliente__3213E83F27101372");

            entity.ToTable("cliente");

            entity.Property(e => e.correo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.direccion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.nombre)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.rut)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.telefono)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.comuna).WithMany(p => p.clientes)
                .HasForeignKey(d => d.comuna_id)
                .HasConstraintName("fk_cliente_comuna");
        });

        modelBuilder.Entity<cliente_vehiculo>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__cliente___3213E83FCE0C1E1D");

            entity.ToTable("cliente_vehiculo");

            entity.HasOne(d => d.cliente).WithMany(p => p.cliente_vehiculos)
                .HasForeignKey(d => d.cliente_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_cliente_vehiculo_cliente");

            entity.HasOne(d => d.vehiculo).WithMany(p => p.cliente_vehiculos)
                .HasForeignKey(d => d.vehiculo_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_cliente_vehiculo_vehiculo");
        });

        modelBuilder.Entity<comuna>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__comuna__3213E83F697DD9FE");

            entity.ToTable("comuna");

            entity.Property(e => e.nombre)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.region).WithMany(p => p.comunas)
                .HasForeignKey(d => d.region_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_comuna_region");
        });

        modelBuilder.Entity<cotizacion>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__cotizaci__3213E83F76D62E3D");

            entity.ToTable("cotizacion");

            entity.Property(e => e.estado)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("Pendiente");
            entity.Property(e => e.fecha_cotizacion).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.cliente).WithMany(p => p.cotizacions)
                .HasForeignKey(d => d.cliente_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_cotizacion_cliente");

            entity.HasOne(d => d.funcionario_cotizacion).WithMany(p => p.cotizacions)
                .HasForeignKey(d => d.funcionario_cotizacion_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_cotizacion_funcionario");
        });

        modelBuilder.Entity<factura>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__factura__3213E83F601D04DE");

            entity.ToTable("factura");

            entity.HasIndex(e => e.folio, "uk_factura_folio").IsUnique();

            entity.Property(e => e.fecha_emision)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.sii_params).HasColumnType("json");

            entity.HasOne(d => d.atencion).WithMany(p => p.facturas)
                .HasForeignKey(d => d.atencion_id)
                .HasConstraintName("fk_factura_atencion");
        });

        modelBuilder.Entity<funcionario>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__funciona__3213E83F5E9B4E84");

            entity.ToTable("funcionario");

            entity.Property(e => e.especialidad)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.rut)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.tipo).WithMany(p => p.funcionarios)
                .HasForeignKey(d => d.tipo_id)
                .HasConstraintName("fk_funcionario_tipo_funcionario");
        });

        modelBuilder.Entity<log_inventario>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__log_inve__3213E83F3CE8046B");

            entity.ToTable("log_inventario");

            entity.Property(e => e.fecha_log)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.nota)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.repuesto_unidades).WithMany(p => p.log_inventarios)
                .HasForeignKey(d => d.repuesto_unidades_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_log_repuesto_stock");

            entity.HasOne(d => d.usuario).WithMany(p => p.log_inventarios)
                .HasForeignKey(d => d.usuario_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_log_usuario");
        });

        modelBuilder.Entity<region>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__region__3213E83F7113C674");

            entity.ToTable("region");

            entity.Property(e => e.nombre)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<repuesto>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__repuesto__3213E83F974C544D");

            entity.ToTable("repuesto");

            entity.Property(e => e.marca)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.nombre)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.sku)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.categoria).WithMany(p => p.repuestos)
                .HasForeignKey(d => d.categoria_id)
                .HasConstraintName("fk_repuesto_categoria");
        });

        modelBuilder.Entity<repuesto_unidades>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__repuesto__3213E83F4C7A1430");

            entity.Property(e => e.stock_reservado).HasDefaultValue(0);

            entity.HasOne(d => d.repuesto).WithMany(p => p.repuesto_unidades)
                .HasForeignKey(d => d.repuesto_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_uniudades_repuesto");

            entity.HasOne(d => d.taller).WithMany(p => p.repuesto_unidades)
                .HasForeignKey(d => d.taller_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_unidades_taller");
        });

        modelBuilder.Entity<servicio>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__servicio__3213E83F54226A4D");

            entity.ToTable("servicio");

            entity.Property(e => e.descripcion)
                .HasMaxLength(500)
                .IsUnicode(false);

            entity.HasOne(d => d.atencion).WithMany(p => p.servicios)
                .HasForeignKey(d => d.atencion_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_servicio_atencion");

            entity.HasOne(d => d.tipo_servicio).WithMany(p => p.servicios)
                .HasForeignKey(d => d.tipo_servicio_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_servicio_tipo_servicio");
        });

        modelBuilder.Entity<taller>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__taller__3213E83FB904D60E");

            entity.ToTable("taller");

            entity.Property(e => e.direccion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.razon_social)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.rut_taller)
                .HasMaxLength(15)
                .IsUnicode(false);

            entity.HasOne(d => d.comuna).WithMany(p => p.tallers)
                .HasForeignKey(d => d.comuna_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_taller_comuna");
        });

        modelBuilder.Entity<taller_repuesto>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__taller_repuesto__3213E83FABCDEF01");

            entity.HasOne(d => d.taller).WithMany(p => p.taller_repuestos)
                .HasForeignKey(d => d.taller_id)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_taller_repuesto_taller");

            entity.HasOne(d => d.repuesto).WithMany(p => p.taller_repuestos)
                .HasForeignKey(d => d.repuesto_id)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_taller_repuesto_repuesto");
        });

        modelBuilder.Entity<tipo_funcionario>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__tipo_fun__3213E83F64214508");

            entity.ToTable("tipo_funcionario");

            entity.Property(e => e.nombre)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<tipo_servicio>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__tipo_ser__3213E83FC284439A");

            entity.ToTable("tipo_servicio");

            entity.Property(e => e.nombre)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<tipo_vehiculo>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__tipo_veh__3213E83FE95E9959");

            entity.ToTable("tipo_vehiculo");

            entity.Property(e => e.nombre)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<usuario>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__usuario__3213E83F0212489F");

            entity.ToTable("usuario");

            entity.HasIndex(e => e.funcionario_id, "UQ__usuario__2D62919B8AAE17AF").IsUnique();

            entity.HasIndex(e => e.cliente_id, "UQ__usuario__47E34D659E9ACA6D").IsUnique();

            entity.Property(e => e.password_hash)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.cliente).WithOne(p => p.usuario)
                .HasForeignKey<usuario>(d => d.cliente_id)
                .HasConstraintName("fk_usuario_cliente");

            entity.HasOne(d => d.funcionario).WithOne(p => p.usuario)
                .HasForeignKey<usuario>(d => d.funcionario_id)
                .HasConstraintName("fk_usuario_funcionario");
        });

        modelBuilder.Entity<vehiculo>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__vehiculo__3213E83FA30926BD");

            entity.ToTable("vehiculo");

            entity.Property(e => e.color)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.patente)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.vin)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.tipo).WithMany(p => p.vehiculos)
                .HasForeignKey(d => d.tipo_id)
                .HasConstraintName("fk_vehiculo_tipo");
        });

        modelBuilder.Entity<servicio_repuesto>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__servicio__3213E83FA8BBE915");

            entity.HasOne(d => d.servicio).WithMany(p => p.servicio_repuestos)
                .HasForeignKey(d => d.servicio_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_servicio_repuesto_servicio");

            entity.HasOne(d => d.repuesto_unidades).WithMany(p => p.servicio_repuestos)
                .HasForeignKey(d => d.repuesto_unidades_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_servicio_repuesto_unidades");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
