export interface TenantInfo {
  id: string
  name: string
  code: string
}

export interface LoginRequest {
  tenantId: string
  email: string
  password: string
}

export interface UserInfo {
  id: string
  email: string
  firstName: string
  lastName: string
  roles: string[]
}

export interface LoginResponse {
  accessToken: string
  refreshToken: string
  expiresIn: number
  user?: UserInfo
}

export interface RoleDto {
  id: string
  name: string
  description?: string | null
}

export interface UserDetailDto {
  id: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  phone?: string | null
  profilePicture?: string | null
  isActive: boolean
  emailConfirmed: boolean
  lastLoginAt?: string | null
  createdAt: string
  roles: RoleDto[]
  permissions: string[]
  staffId?: string | null
  staffNumber?: string | null
  guardianId?: string | null
}
