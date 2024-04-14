# variable "client_id" {
#   description = "The client ID of the Service Principal"
#   type        = string
#   sensitive   = true
# }

# variable "client_secret" {
#   description = "The client secret of the Service Principal"
#   type        = string
#   sensitive   = true
# }
variable "admin_password" {
  type        = string
  description = "The administrator password of the SQL logical server."
  sensitive   = true
  default     = null
}
