terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "3.98.0"
    }
    docker = {
      source  = "kreuzwerker/docker"
      version = "~> 2.15.0"
    }
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "2.29.0"
    }
    azapi = {
      source  = "Azure/azapi"
      version = "1.12.1"
    }
    random = {
      source  = "hashicorp/random"
      version = "~>3.0"
    }
  }
}

provider "kubernetes" {
  host                   = azurerm_kubernetes_cluster.k8s-frontend.kube_config[0].host
  client_certificate     = base64decode(azurerm_kubernetes_cluster.k8s-frontend.kube_config[0].client_certificate)
  client_key             = base64decode(azurerm_kubernetes_cluster.k8s-frontend.kube_config[0].client_key)
  cluster_ca_certificate = base64decode(azurerm_kubernetes_cluster.k8s-frontend.kube_config[0].cluster_ca_certificate)
}
