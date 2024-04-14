resource "azurerm_kubernetes_cluster" "k8s-frontend" {
  name                = "hcportal-aks"
  location            = data.azurerm_resource_group.hcportal_resource_group.location
  resource_group_name = data.azurerm_resource_group.hcportal_resource_group.name
  dns_prefix          = "dns-htportal"

  identity {
    type = "SystemAssigned"
  }

  default_node_pool {
    name       = "agentpool"
    vm_size    = "Standard_D2_v2"
    node_count = 2
  }
  linux_profile {
    admin_username = "azureadmin"

    ssh_key {
      key_data = jsondecode(azapi_resource_action.ssh_public_key_gen.output).publicKey
    }
  }
  network_profile {
    network_plugin    = "kubenet"
    load_balancer_sku = "standard"
  }

  tags = {
    Environment  = "DEV"
    ResourceType = "AKS"
    Area         = "Frontend"
  }
}

resource "kubernetes_secret" "hcportal_fe_credentials" {
  metadata {
    name = "acr-credentials"
  }

  data = {
    ".dockerconfigjson" = jsonencode({
      "auths" = {
        "hcportalcontainerregistry.azurecr.io" = {
          "username" = "hcportalcontainerregistry"
          "password" = "Z3qlUyTU0SNzpes9IPwLBHKyt5jLO/ap9poGHLlfom+ACRAKX7F8"
        }
      }
    })
  }

  type = "kubernetes.io/dockerconfigjson"
}

resource "kubernetes_deployment" "fe_deployment" {
  metadata {
    name = "hcportal-fe-deployment"
    labels = {
      project = "hcportal-fe-deployment"
    }
  }

  spec {
    replicas = 1

    selector {
      match_labels = {
        App = "hcportal-fe"
      }
    }

    template {
      metadata {
        labels = {
          App = "hcportal-fe"
        }
      }

      spec {
        image_pull_secrets {
          name = kubernetes_secret.hcportal_fe_credentials.metadata[0].name
        }

        container {
          image = "hcportalcontainerregistry.azurecr.io/hcportal-fe-image:latest"
          name  = "hcportal-fe"

          resources {
            limits = {
              cpu    = "1"
              memory = "512Mi"
            }
            requests = {
              cpu    = "10m"
              memory = "50Mi"
            }
          }

          env {
            name  = "VITE_SET_BACKEND_URL"
            value = "https://${azurerm_linux_function_app.be_linux_function_app.default_hostname}"
          }


          liveness_probe {
            http_get {
              path = "/"
              port = 80
            }

            initial_delay_seconds = 3
            period_seconds        = 3
          }

          port {
            container_port = 80
          }
        }
      }
    }
  }

  depends_on = [azurerm_linux_function_app.be_linux_function_app]
}

resource "kubernetes_service" "fe_service" {
  metadata {
    name = "hcportal-fe-service"
  }

  spec {
    selector = {
      App = "hcportal-fe"
    }

    port {
      port        = 80
      target_port = 80
    }

    type = "LoadBalancer"
  }

  depends_on = [kubernetes_deployment.fe_deployment]
}
